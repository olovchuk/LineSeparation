using System.Diagnostics;

namespace LineSeparation.Models;

public static class TortoiseCommand
{
    public static async void OpenCommit(string repositoryDirectory)
    {
        var tortoiseGitCommit = new ProcessStartInfo
        {
            FileName = "TortoiseGitProc.exe",
            Arguments = $"/command:commit /path:\"{repositoryDirectory}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal
        };

        // Start the process
        using var process = new Process();
        process.StartInfo = tortoiseGitCommit;
        process.Start();
        await process.WaitForExitAsync();
    }
    
    public static async void OpenDiffFile(string fileFullPath)
    {
        var tortoiseGitCommit = new ProcessStartInfo
        {
            FileName = "TortoiseGitProc.exe",
            Arguments = $"/command:diff /path:\"{fileFullPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal
        };

        // Start the process
        using var process = new Process();
        process.StartInfo = tortoiseGitCommit;
        process.Start();
        await process.WaitForExitAsync();
    }
}