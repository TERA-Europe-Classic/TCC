namespace TCC.Tests;

using System.Reflection;
using TCC;
using TCC.Settings;

public class SettingsPersistenceTests
{
    [Fact]
    public void SettingsSaveWritesSynchronouslyInsteadOfQueuingPastShutdown()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "Settings",
            "SettingsContainer.cs"));

        Assert.DoesNotContain("InvokeAsync(() => new JsonSettingsWriter().Save())", source);
        Assert.Contains("App.BaseDispatcher.Invoke(() => new JsonSettingsWriter().Save())", source);
    }

    [Fact]
    public void FreshSettingsUseBuffWindowShowAllDefaultForSelfAbnormalities()
    {
        var settings = new TCC.Settings.SettingsContainer();

        Assert.True(settings.BuffWindowSettings.ShowAll);
        Assert.True(settings.AbnormalitySettings.Self.ShowAll);
    }

    [Fact]
    public void ChatSettingCannotBeReenabledFromPersistedConfig()
    {
        var settings = new TCC.Settings.SettingsContainer
        {
            ChatEnabled = true
        };

        Assert.False(settings.ChatEnabled);
        Assert.False(settings.ChatSettings.Enabled);
    }

    [Fact]
    public void SettingsLoadFallsBackToBackupWhenPrimaryJsonIsUnreadable()
    {
        using var temp = new TempSettingsFile();
        File.WriteAllText(temp.Path, "{ not valid json");
        File.WriteAllText(temp.BackupPath, MinimalSettingsJson(37));

        var loaded = new JsonSettingsReader().LoadSettings(temp.Path);

        Assert.Equal(37, loaded.FontSize);
    }

    [Fact]
    public void SettingsSaveReplacesAtomicallyAndKeepsPreviousFileAsBackup()
    {
        using var temp = new TempSettingsFile();
        var newSettings = new SettingsContainer { FontSize = 38 };
        File.WriteAllText(temp.Path, MinimalSettingsJson(21));
        SetAppSettings(newSettings);
        SettingsContainer.SettingsOverride = temp.Path;

        try
        {
            new JsonSettingsWriter().Save();

            var saved = new JsonSettingsReader().LoadSettings(temp.Path);
            var backup = new JsonSettingsReader().LoadSettings(temp.BackupPath);
            Assert.Equal(38, saved.FontSize);
            Assert.Equal(21, backup.FontSize);
        }
        finally
        {
            SettingsContainer.SettingsOverride = "";
        }
    }

    [Fact]
    public void SettingsControlsPersistValueChangesImmediately()
    {
        var root = FindRepoRoot().FullName;
        var controls = new[]
        {
            "BoolSetting.xaml.cs",
            "CheckboxSetting.xaml.cs",
            "SelectionSetting.xaml.cs",
            "ValueSetting.xaml.cs"
        };

        foreach (var control in controls)
        {
            var source = File.ReadAllText(Path.Combine(root, "TCC.Core", "UI", "Controls", "Settings", control));
            Assert.Contains("SaveSettingsIfReady", source);
            Assert.Contains("App.Settings.Save();", source);
        }
    }

    private static DirectoryInfo FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "TCC.sln")))
        {
            current = current.Parent;
        }

        return current ?? throw new DirectoryNotFoundException("Could not find TCC.sln from test output.");
    }

    private static void SetAppSettings(SettingsContainer settings)
    {
        typeof(App).GetProperty(nameof(App.Settings), BindingFlags.Public | BindingFlags.Static)!
            .SetValue(null, settings);
    }

    private static string MinimalSettingsJson(int fontSize)
    {
        return $$"""
        {
          "FontSize": {{fontSize}},
          "LanguageOverride": 0
        }
        """;
    }

    private sealed class TempSettingsFile : IDisposable
    {
        private readonly string _directory;

        public string Path { get; }
        public string BackupPath => Path + ".bak";

        public TempSettingsFile()
        {
            _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tcc-settings-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            Path = System.IO.Path.Combine(_directory, SettingsGlobals.SettingsFileName);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }
    }
}
