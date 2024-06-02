using System.Windows.Media;

namespace LineSeparation.Models.Common;

public class FixedFileInfo
{
    public string FilePath { get; set; }
    public int OriginalHead { get; set; }
    public FileStats OriginalStatus { get; set; }
    public int NewHead { get; set; }
    public FileStats NewStatus { get; set; }
    public FileStats Diff { get; set; }
    public bool IsFixed { get; set; }

    public SolidColorBrush StatusColor => IsFixed ? Brushes.Green : Brushes.Brown;
}