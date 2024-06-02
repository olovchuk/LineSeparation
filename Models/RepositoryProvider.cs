using System.IO;
using LibGit2Sharp;

namespace LineSeparation.Models;

public class RepositoryProvider(string directory)
{
    private readonly Repository repository = new(directory);

    public IEnumerable<string> GetPendingFiles()
    {
        var list = new List<string>();
        var status = this.repository.RetrieveStatus().Where(x => x.State != FileStatus.Ignored);
        foreach (var entry in status)
        {
            var qualifies = false;
            if ((entry.State & FileStatus.ModifiedInWorkdir) == FileStatus.ModifiedInWorkdir)
            {
                qualifies = true;
            }
            else if ((entry.State & FileStatus.ModifiedInIndex) == FileStatus.ModifiedInIndex)
            {
                qualifies = true;
            }

            if (qualifies && !list.Contains(entry.FilePath))
            {
                list.Add(entry.FilePath);
                yield return entry.FilePath;
            }
        }
    }

    public Stream? GetTipStream(string path)
    {
        var tip = this.repository.Head.Tip[path];
        if (tip == null)
        {
            return null;
        }

        return tip.Target is not Blob blob ? null : blob.GetContentStream();
    }

    public Stream OpenLocalFile(string path, FileMode mode, FileAccess access, FileShare share)
    {
        return new FileStream($"{directory}\\{path}", mode, access, share);
    }

    public void LocalFileCopy(string from, string to)
    {
        File.Copy(from, to);
    }
}