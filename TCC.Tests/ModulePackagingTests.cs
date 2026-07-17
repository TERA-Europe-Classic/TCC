using System.Xml.Linq;

namespace TCC.Tests;

public class ModulePackagingTests
{
    /// The Classic+ launcher reads TCC's UI-remover payloads from
    /// `<install>/client/gpk/x64/` — the publish rules must keep shipping
    /// exactly that layout even though the toolbox JS module is gone.
    [Fact]
    public void GpkPayloadsArePublishedAtClientGpkX64()
    {
        var project = XDocument.Load(Path.Combine(FindRepoRoot().FullName, "TCC.Core", "TCC.Core.csproj"));
        var gpkContent = project
            .Descendants("Content")
            .Single(element => (string?)element.Attribute("Include") == @"Module\client\gpk\x64\*.gpk");

        Assert.Equal(@"client\gpk\x64\%(Filename)%(Extension)", (string?)gpkContent.Attribute("Link"));
        Assert.DoesNotContain(
            project.Descendants("Content"),
            element => (string?)element.Attribute("Include") == @"Module\**\*.*");
    }

    /// All eight Classic+ removers must exist as x64 payloads. The launcher's
    /// remover reconcile sources each of these by filename.
    [Theory]
    [InlineData("S1UI_Abnormality.gpk")]
    [InlineData("S1UI_CharacterWindow.gpk")]
    [InlineData("S1UI_DistributionWindow.gpk")]
    [InlineData("S1UI_GageBoss.gpk")]
    [InlineData("S1UI_PartyWindow.gpk")]
    [InlineData("S1UI_PartyWindowRaidInfo.gpk")]
    [InlineData("S1UI_ProgressBar.gpk")]
    [InlineData("S1UI_TargetInfo.gpk")]
    public void ClassicPlusShipsAllX64RemoverPayloads(string fileName)
    {
        var payload = Path.Combine(
            FindRepoRoot().FullName, "TCC.Core", "Module", "client", "gpk", "x64", fileName);

        Assert.True(File.Exists(payload), $"missing launcher remover payload: {payload}");
    }

    /// The tera-toolbox module (JS loader, network stub, x86 payloads) is
    /// dead in Classic+ — the launcher owns GPK deployment and spawns
    /// TCC.exe directly. Only the x64 remover payloads remain under Module/.
    [Fact]
    public void ClassicPlusDoesNotShipToolboxModuleCode()
    {
        var moduleRoot = Path.Combine(FindRepoRoot().FullName, "TCC.Core", "Module");

        Assert.Empty(Directory.GetFiles(moduleRoot, "*.js", SearchOption.AllDirectories));
        Assert.Empty(Directory.GetFiles(moduleRoot, "*.json", SearchOption.AllDirectories));
        Assert.False(
            Directory.Exists(Path.Combine(moduleRoot, "client", "gpk", "x86")),
            "x86 remover payloads have no consumer (launcher uses x64; toolbox path removed)");
    }

    [Theory]
    [InlineData("S1UI_Chat2.gpk")]
    [InlineData("S1UI_PartyBoard.gpk")]
    [InlineData("S1UI_PartyBoardMemberInfo.gpk")]
    public void ClassicPlusDoesNotShipDeadChatOrLfgGpkRemovers(string fileName)
    {
        var moduleRoot = Path.Combine(FindRepoRoot().FullName, "TCC.Core", "Module");
        var files = Directory.GetFiles(moduleRoot, fileName, SearchOption.AllDirectories);

        Assert.Empty(files);
    }

    [Fact]
    public void AppDoesNotStartChatOrLfgRuntimeSurfaces()
    {
        var root = FindRepoRoot().FullName;
        var windowManager = File.ReadAllText(Path.Combine(root, "TCC.Core", "UI", "WindowManager.cs"));
        var game = File.ReadAllText(Path.Combine(root, "TCC.Core", "Game.cs"));
        var app = File.ReadAllText(Path.Combine(root, "TCC.Core", "App.xaml.cs"));
        var chatManager = File.ReadAllText(Path.Combine(root, "TCC.Core", "ViewModels", "ChatManager.cs"));

        Assert.DoesNotContain("ChatManager.Start()", windowManager);
        Assert.DoesNotContain("new LfgListWindow", windowManager);
        Assert.DoesNotContain("Hook<S_CHAT>", game);
        Assert.DoesNotContain("Hook<S_PRIVATE_CHAT>", game);
        Assert.DoesNotContain("Hook<S_WHISPER>", game);
        Assert.DoesNotContain("Hook<S_CHAT>", chatManager);
        Assert.DoesNotContain("Hook<S_PRIVATE_CHAT>", chatManager);
        Assert.DoesNotContain("Hook<S_WHISPER>", chatManager);
        Assert.DoesNotContain("Hook<S_PARTY_MEMBER_INFO>", chatManager);
        Assert.DoesNotContain("Settings.LfgWindowSettings.Enabled", app);
        Assert.DoesNotContain("Settings.ShowIngameChat", app);
        Assert.DoesNotContain("Settings.ChatEnabled", app);
    }

    [Fact]
    public void SettingsAndFloatingButtonDoNotExposeDeadChatOrLfgControls()
    {
        var root = FindRepoRoot().FullName;
        var settings = File.ReadAllText(Path.Combine(root, "TCC.Core", "UI", "Windows", "SettingsWindow.xaml"));
        var floatingButton = File.ReadAllText(Path.Combine(root, "TCC.Core", "UI", "Windows", "Widgets", "FloatingButtonWindow.xaml"));
        var floatingButtonVm = File.ReadAllText(Path.Combine(root, "TCC.Core", "UI", "Windows", "Widgets", "FloatingButtonViewModel.cs"));
        var settingsVm = File.ReadAllText(Path.Combine(root, "TCC.Core", "ViewModels", "SettingsWindowViewModel.cs"));

        Assert.DoesNotContain("Show ingame chat messages", settings);
        Assert.DoesNotContain("Toggle clickable chat", settings);
        Assert.DoesNotContain("LFG window", settings);
        Assert.DoesNotContain("S1UI_Chat2.gpk", settings);
        Assert.DoesNotContain("OpenConfigureLfgWindowCommand", settings);
        Assert.DoesNotContain("OpenLfgCommand", floatingButton);
        Assert.DoesNotContain("OpenLfgCommand", floatingButtonVm);
        Assert.DoesNotContain("TccChatEnabled", settingsVm);
        Assert.DoesNotContain("OpenConfigureLfgWindowCommand", settingsVm);
    }

    [Fact]
    public void AppDoesNotSendDeadChatOrLfgPacketActions()
    {
        var root = FindRepoRoot().FullName;
        var templates = File.ReadAllText(Path.Combine(root, "TCC.Core", "ResourceDictionaries", "DataTemplates.xaml.cs"));
        var actionPiece = File.ReadAllText(Path.Combine(root, "TCC.Core", "Data", "Chat", "ActionMessagePiece.cs"));
        var user = File.ReadAllText(Path.Combine(root, "TCC.Core", "Data", "Pc", "User.cs"));

        Assert.DoesNotContain("RegisterListing", templates);
        Assert.DoesNotContain("RequestListings", templates);
        Assert.DoesNotContain("ChatLinkAction(", actionPiece);
        Assert.DoesNotContain("DeclineUserGroupApply", user);
        Assert.DoesNotContain("RequestListingCandidates", user);
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
