using System.IO;
using System.Text.Json;
using System.Windows;

namespace LineSeparation.Configuration;

public class ConfigInfo
{
    private const string ConfigInfoFilePath = "config.json";

    public string GitRepositoryPath { get; set; } = string.Empty;
    public bool IsExecute { get; set; }
    public bool IsBackup { get; set; }
    public bool IsExpandView { get; set; }

    public void Init()
    {
        try
        {
            if (!File.Exists(ConfigInfoFilePath))
            {
                SetDefault();
                Save();
            }
            else
            {
                var json = File.ReadAllText(ConfigInfoFilePath);
                var data = JsonSerializer.Deserialize<ConfigInfo>(json);
                this.GitRepositoryPath = data!.GitRepositoryPath;
                this.IsExecute = data.IsExecute;
                this.IsBackup = data.IsBackup;
                this.IsExpandView = data.IsExpandView;
            }
        }
        catch (Exception ex)
        {
            SetDefault();
            MessageBox.Show("The configuration could not be downloaded. Set the default settings", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SetDefault()
    {
        this.GitRepositoryPath = "Not selected";
        this.IsExecute = true;
        this.IsBackup = false;
        this.IsExpandView = false;
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this);
        if (!File.Exists(ConfigInfoFilePath))
        {
            using var FsCreate = File.Create(ConfigInfoFilePath);
            FsCreate.Close();
        }

        File.WriteAllText(ConfigInfoFilePath, json);
    }
}