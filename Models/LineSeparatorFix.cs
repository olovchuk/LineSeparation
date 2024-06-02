using System.IO;
using System.Text;
using LineSeparation.Models.Common;
using Spectre.Console.Cli;

namespace LineSeparation.Models;

public class LineSeparatorFix(RepositoryProvider provider)
{
    private const char CR = '\r';
    private const char LF = '\n';
    private readonly RepositoryProvider _provider = provider;

    public class Settings : CommandSettings
    {
        public List<string> Paths { get; set; } = new();
        public bool Execute { get; set; }
        public bool Backup { get; set; }
    }

    public (int NotFixedFilesCount, List<FixedFileInfo> FixedFilesInfo) Execute(Settings settings)
    {
        var paths = settings.Paths?.ToList() ?? new List<string>();
        bool isExplicit = true;
        if (paths.Count == 0)
        {
            isExplicit = false;
            paths = this._provider.GetPendingFiles().ToList();
        }

        var things = paths.Select(path => new FileOperation(path)
            { IsExplicit = isExplicit, IsPendingChange = !isExplicit, }).ToList();

        var fixedFilesInfo = new List<FixedFileInfo>();
        int problemFiles = 0, fixedFiles = 0;
        foreach (var thing in things)
        {
            var result = ExecuteFile(thing, settings.Execute, settings.Backup);
            if (result.fixedFileInfo != null)
                fixedFilesInfo.Add(result.fixedFileInfo);
            problemFiles += result.Item1.CanBeFixed ? 1 : 0;
            fixedFiles += result.Item1.Proceed ? 1 : 0;
        }

        return (problemFiles - fixedFiles, fixedFilesInfo);
    }

    private static void SpecialReadLines(TextReader reader, List<string> lines)
    {
        int value, previousValue = 0;
        var builder = new StringBuilder();
        while ((value = reader.Read()) >= 0)
        {
            var current = (char)value;
            if (previousValue == CR && value == LF)
            {
                builder.Append(current);
                lines.Add(builder.ToString());
                builder.Clear();
            }
            else if (previousValue != CR && value == LF)
            {
                builder.Append(current);
                lines.Add(builder.ToString());
                builder.Clear();
            }
            else if (previousValue == CR)
            {
                lines.Add(builder.ToString());
                builder.Clear();
                builder.Append(current);
            }
            else
            {
                builder.Append(current);
            }

            previousValue = value;
        }

        if (builder.Length > 0)
        {
            lines.Add(builder.ToString());
        }
    }

    private static Ending GetLineType(string line)
    {
        if (line.Length == 0)
        {
            return Ending.Other;
        }

        if (line.Length >= 2)
        {
            if (line[^2] == CR && line[^1] == LF)
            {
                return Ending.CrLf;
            }
        }

        if (line[^1] == CR)
        {
            return Ending.Cr;
        }
        else if (line[^1] == LF)
        {
            return Ending.Lf;
        }

        return Ending.Other;
    }

    public static FileStats GetStats(Stream stream, Encoding encoding)
    {
        var newLines = new List<string>();
        using (var reader = new StreamReader(stream, encoding))
        {
            SpecialReadLines(reader, newLines);
        }

        return GetStats(newLines);
    }

    private static FileStats GetStats(List<string> lines)
    {
        int unix = 0, windows = 0, mac = 0, other = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var type = GetLineType(line);
            switch (type)
            {
                case Ending.CrLf:
                    windows++;
                    break;
                case Ending.Cr:
                    mac++;
                    break;
                case Ending.Lf:
                    unix++;
                    break;
                default:
                    if ((i + 1) < lines.Count)
                    {
                        other++;
                    }

                    break;
            }
        }

        return new FileStats(unix, windows, mac, other);
    }

    private bool SearchLine(string searchLine, int derivedIndex, List<string> originalLines, out int foundIndex,
        out string foundLine)
    {
        var lineTrimmed = searchLine.Trim();

        if (originalLines.Count > derivedIndex)
        {
            var trimmed = originalLines[derivedIndex].Trim();
            if (string.Equals(lineTrimmed, trimmed))
            {
                foundIndex = derivedIndex;
                foundLine = originalLines[derivedIndex];
                return true;
            }
        }

        var delta = 10;
        var start = Math.Max(derivedIndex - delta, 0);
        var end = Math.Min(derivedIndex + delta, originalLines.Count - 1);

        for (int i = start; i <= end; i++)
        {
            var trimmed = originalLines[i].Trim();
            if (string.Equals(lineTrimmed, trimmed))
            {
                foundIndex = i;
                foundLine = originalLines[i];
                return true;
            }
        }

        foundIndex = -1;
        foundLine = null;
        return false;
    }

    private void Append(string line, Ending lineType, Ending newType, StringBuilder writer)
    {
        if (lineType == newType)
        {
            writer.Append(line);
            return;
        }

        if (lineType == Ending.CrLf)
        {
            writer.Append(line, 0, line.Length - 2);
        }
        else if (lineType == Ending.Cr || lineType == Ending.Lf)
        {
            writer.Append(line, 0, line.Length - 1);
        }

        if (newType == Ending.CrLf)
        {
            writer.Append(CR);
            writer.Append(LF);
        }
        else if (newType == Ending.Cr)
        {
            writer.Append(CR);
        }
        else if (newType == Ending.Lf)
        {
            writer.Append(LF);
        }
    }

    public (FileOperation fileOperation, FixedFileInfo? fixedFileInfo) ExecuteFile(FileOperation result, bool execute,
        bool backup)
    {
        var path = result.Path;
        var encoding = Encoding.UTF8;
        var preference = Ending.Lf;
        FixedFileInfo? fixedFileInfo = null;

        var originalLines = new List<string>();
        result.IsFoundInGit = false;
        var tipStream = this._provider.GetTipStream(path);
        if (tipStream != null)
        {
            result.IsFoundInGit = true;
        }
        else
        {
            return (result, fixedFileInfo);
        }

        // read file from git database
        using (tipStream)
        using (var reader = new StreamReader(tipStream, encoding))
        {
            SpecialReadLines(reader, originalLines);
        }

        // read local file
        var newLines = new List<string>();
        using (var file = this._provider.OpenLocalFile(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var reader = new StreamReader(file, encoding))
        {
            SpecialReadLines(reader, newLines);
        }

        // compare line endings
        var originalStats = GetStats(originalLines);
        result.OriginalStats = originalStats;
        var newStats = GetStats(newLines);
        result.LocalStats = newStats;

        // regenerate fixed file in-memory
        var newContents = new StringBuilder();
        var derivedIndex = -1;
        int foundIndex;
        string foundLine;
        foreach (var myLine in newLines)
        {
            var myType = GetLineType(myLine);

            if (SearchLine(myLine, derivedIndex + 1, originalLines, out foundIndex, out foundLine))
            {
                var foundType = GetLineType(foundLine);
                derivedIndex = foundIndex;
                Append(myLine, myType, foundType, newContents);
            }
            else
            {
                derivedIndex++;
                Append(myLine, myType, preference, newContents);
            }
        }

        // analyze the new file
        var newNewLines = new List<string>();
        SpecialReadLines(new StringReader(newContents.ToString()), newNewLines);
        var newNewStats = GetStats(newNewLines);
        result.FixedStats = newNewStats;

        var diff = newStats.Diff(newNewStats);

        if (diff.AbsoluteTotal > 0)
        {
            result.CanBeFixed = true;
        }

        if (result.IsExplicit || result.CanBeFixed)
        {
            result.IsDisplayed = true;
            fixedFileInfo = new FixedFileInfo
            {
                FilePath = path,
                OriginalHead = originalStats.Total,
                OriginalStatus = originalStats,
                NewHead = newNewStats.Total,
                NewStatus = newNewStats,
                Diff = diff
            };
        }

        if (!result.CanBeFixed)
        {
            return (result, fixedFileInfo);
        }

        // write fixed file on disk, if desired
        if (execute)
        {
            result.Proceed = true;
            /*if (backup)
            {
                this._provider.LocalFileCopy(path, path + ".backup");
            }*/

            using (var file = this._provider.OpenLocalFile(path, FileMode.Open, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(file, encoding))
            {
                foreach (var line in newNewLines)
                {
                    writer.Write(line);
                }

                writer.Flush();
                file.SetLength(file.Position);
                file.Flush();
            }

            if (fixedFileInfo != null)
                fixedFileInfo.IsFixed = true;
        }

        return (result, fixedFileInfo);
    }
}