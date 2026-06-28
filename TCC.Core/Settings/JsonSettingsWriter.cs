using System.IO;
using System.Windows;
using Newtonsoft.Json;
using TCC.UI.Windows;
using TCC.Utilities;
using TCC.Utils;

namespace TCC.Settings;

public class JsonSettingsWriter : SettingsWriterBase
{
    public JsonSettingsWriter()
    {
        FileName = SettingsGlobals.SettingsFileName;
    }
    public override void Save()
    {
        var json = JsonConvert.SerializeObject(App.Settings, Formatting.Indented, TccUtils.GetDefaultJsonSerializerSettings());
        var savePath = SettingsContainer.SettingsOverride == ""
            ? Path.Combine(App.BasePath, FileName)
            : SettingsContainer.SettingsOverride;
        var tempPath = $"{savePath}.tmp";
        var backupPath = SettingsGlobals.GetBackupPath(savePath);
        var previousBackupPath = $"{backupPath}.previous";

        try
        {
            var directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(tempPath, json);
            ReplaceSettingsFile(tempPath, savePath, backupPath, previousBackupPath);
        }
        catch (IOException ex)
        {
            Log.F($"Failed to save settings to {savePath}: {ex}");
            var res = TccMessageBox.Show("TCC", SR.CannotSaveSettings(ex.Message), MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes) Save();
        }
        finally
        {
            TryDelete(tempPath);
            TryDelete(previousBackupPath);
        }
    }

    private static void ReplaceSettingsFile(string tempPath, string savePath, string backupPath, string previousBackupPath)
    {
        if (!File.Exists(savePath))
        {
            File.Move(tempPath, savePath, true);
            return;
        }

        if (File.Exists(backupPath))
        {
            File.Move(backupPath, previousBackupPath, true);
        }

        try
        {
            File.Replace(tempPath, savePath, backupPath, true);
        }
        catch
        {
            if (!File.Exists(backupPath) && File.Exists(previousBackupPath))
            {
                File.Move(previousBackupPath, backupPath, true);
            }

            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // ignored
        }
    }
}
