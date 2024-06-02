using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LibGit2Sharp;
using LineSeparation.Configuration;
using LineSeparation.Models;
using LineSeparation.Models.Common;
using Microsoft.Win32;

namespace LineSeparation;

public partial class MainWindow : Window
{
    private new readonly bool IsInitialized;
    private readonly ConfigInfo _configInfo = new();
    private readonly LineSeparatorFix.Settings Settings;
    private LineSeparatorFix? LineSeparatorFix;
    private List<FixedFileInfo> FixedFilesInfo = new();

    public MainWindow()
    {
        InitializeComponent();

        _configInfo.Init();
        Settings = new LineSeparatorFix.Settings
        {
            Paths = [],
            Execute = _configInfo.IsExecute,
            Backup = _configInfo.IsBackup
        };

        if (!Path.Exists(_configInfo.GitRepositoryPath))
        {
            CurrentGitRepositoryPath.Foreground = Brushes.Brown;
            CurrentGitRepositoryPath.FontWeight = FontWeights.Bold;
            LineSeparatorFix = null;
        }
        else
        {
            var provider = new RepositoryProvider(_configInfo.GitRepositoryPath);
            LineSeparatorFix = new LineSeparatorFix(provider);
        }

        CurrentGitRepositoryPath.Text = _configInfo.GitRepositoryPath;
        RbExecute.IsChecked = _configInfo.IsExecute;
        RbBackup.IsChecked = _configInfo.IsBackup;

        IsInitialized = true;
    }

    private void ChangeGitRepositoryPath_OnClick(object sender, RoutedEventArgs e)
    {
        var openFolderDialog = new OpenFolderDialog();
        var result = openFolderDialog.ShowDialog();
        if (!result.HasValue || !result.Value) return;

        try
        {
            var repository = new Repository(openFolderDialog.FolderName);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var provider = new RepositoryProvider(openFolderDialog.FolderName);
        LineSeparatorFix = new LineSeparatorFix(provider);
        CurrentGitRepositoryPath.Text = openFolderDialog.FolderName;
        CurrentGitRepositoryPath.ToolTip = openFolderDialog.FolderName;
        CurrentGitRepositoryPath.Foreground = Brushes.Black;
        _configInfo.GitRepositoryPath = openFolderDialog.FolderName;
        _configInfo.Save();
    }

    private void Run_OnClick(object sender, RoutedEventArgs e)
    {
        if (LineSeparatorFix == null)
            return;

        var result = LineSeparatorFix.Execute(Settings);
        FixedFilesInfo = result.FixedFilesInfo;
        if (result.NotFixedFilesCount == 0)
        {
            if (result.FixedFilesInfo.Count == 0)
                Status.Text = "Files do not need to be fixed !";
            else
                Status.Text = $"All files are successfully fixed ! ({result.FixedFilesInfo.Count})";
            Status.Foreground = Brushes.Green;
            Status.FontWeight = FontWeights.Bold;
        }
        else
        {
            if (_configInfo.IsBackup)
            {
                Status.Text = $"Successful backups ! ({result.FixedFilesInfo.Count})";
                Status.Foreground = Brushes.Green;
                Status.FontWeight = FontWeights.Bold;
            }
            else
            {
                Status.Text = $"Some files were not fixed ! ({result.NotFixedFilesCount})";
                Status.Foreground = Brushes.Brown;
                Status.FontWeight = FontWeights.Bold;
            }
        }

        DataContext = FixedFilesInfo;
    }

    private void RbExecute_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized || !RbExecute.IsChecked.HasValue)
            return;

        Settings.Execute = RbExecute.IsChecked.Value;
        Settings.Backup = !RbExecute.IsChecked.Value;
        _configInfo.IsExecute = RbExecute.IsChecked.Value;
        _configInfo.IsBackup = !RbExecute.IsChecked.Value;
        _configInfo.Save();
    }

    private void RbBackup_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized || !RbBackup.IsChecked.HasValue)
            return;

        Settings.Execute = !RbBackup.IsChecked.Value;
        Settings.Backup = RbBackup.IsChecked.Value;
        _configInfo.IsExecute = !RbBackup.IsChecked.Value;
        _configInfo.IsBackup = RbBackup.IsChecked.Value;
        _configInfo.Save();
    }

    private void CbExpandView_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized || !CbExpandView.IsChecked.HasValue)
            return;

        _configInfo.IsExpandView = CbExpandView.IsChecked.Value;
        _configInfo.Save();
    }

    private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        try
        {
            var selectedFixedFileInfo = (FixedFileInfo)((ListBox)sender).SelectedItem;
            var fileFullPath = $"{_configInfo.GitRepositoryPath}\\{selectedFixedFileInfo.FilePath}";
            if (File.Exists(fileFullPath))
            {
                TortoiseCommand.OpenDiffFile(fileFullPath);
            }
        }
        catch (Exception exception)
        {
            //ignore
        }
    }

    private void OpenCommit_OnClick(object sender, RoutedEventArgs e)
    {
        TortoiseCommand.OpenCommit(_configInfo.GitRepositoryPath);
    }
}