using System.Text;

namespace LineSeparation.Models.Common;

public class FileStats(int unixLines, int windowsLines, int macLines, int otherLines)
{
    public int UnixLines { get; } = unixLines;
    public int WindowsLines { get; } = windowsLines;
    public int MacLines { get; } = macLines;
    public int OtherLines { get; } = otherLines;

    public static FileStats Unix(int lines)
    {
        return new FileStats(lines, 0, 0, 0);
    }

    public static FileStats Windows(int lines)
    {
        return new FileStats(0, lines, 0, 0);
    }

    public static FileStats Mac(int lines)
    {
        return new FileStats(0, 0, lines, 0);
    }

    public int Total => this.UnixLines + this.WindowsLines + this.MacLines + this.OtherLines;

    public int AbsoluteTotal =>
        Math.Abs(this.UnixLines) + Math.Abs(this.WindowsLines) + Math.Abs(this.MacLines) +
        Math.Abs(this.OtherLines);

    public bool IsMixed
    {
        get
        {
            var status = 0;
            if (this.UnixLines > 0)
            {
                status++;
            }

            if (this.WindowsLines > 0)
            {
                status++;
            }

            if (this.MacLines > 0)
            {
                status++;
            }

            if (this.OtherLines > 0)
            {
                status++;
            }

            return status > 1;
        }
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        var sep = " ";

        if (this.UnixLines != 0)
        {
            builder.Append(sep);
            builder.Append("LF:");
            builder.Append(this.UnixLines);
            sep = ";";
        }

        if (this.WindowsLines != 0)
        {
            builder.Append(sep);
            builder.Append("CRLF:");
            builder.Append(this.WindowsLines);
            sep = ";";
        }

        if (this.MacLines != 0)
        {
            builder.Append(sep);
            builder.Append("CR:");
            builder.Append(this.MacLines);
            sep = ";";
        }

        if (this.OtherLines != 0)
        {
            builder.Append(sep);
            builder.Append("XX:");
            builder.Append(this.OtherLines);
            sep = ";";
        }

        return builder.ToString();
    }

    public FileStats Diff(FileStats source)
    {
        return new FileStats(
            source.UnixLines - this.UnixLines,
            source.WindowsLines - this.WindowsLines,
            source.MacLines - this.MacLines,
            source.OtherLines - this.OtherLines);
    }
}