namespace TCC.Tests;

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

    private static DirectoryInfo FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "TCC.sln")))
        {
            current = current.Parent;
        }

        return current ?? throw new DirectoryNotFoundException("Could not find TCC.sln from test output.");
    }
}
