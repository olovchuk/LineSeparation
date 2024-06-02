namespace LineSeparation.Models.Common;

public class FileOperation(string path)
{
    public string Path { get; } = path;
    public bool IsFoundInGit { get; set; }
    public FileStats OriginalStats { get; set; }
    public FileStats LocalStats { get; set; }
    public FileStats FixedStats { get; set; }
    public bool Proceed { get; set; }
    public bool CanBeFixed { get; set; }
    public bool IsPendingChange { get; set; }
    public bool IsExplicit { get; set; }
    public bool IsDisplayed { get; set; }
}