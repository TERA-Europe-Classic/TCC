using System.Xml.Linq;

namespace TCC.Tests;

public class ModulePackagingTests
{
    [Fact]
    public void ModuleFilesArePublishedAtToolboxModuleRoot()
    {
        var project = XDocument.Load(Path.Combine(FindRepoRoot().FullName, "TCC.Core", "TCC.Core.csproj"));
        var moduleContent = project
            .Descendants("Content")
            .Single(element => (string?)element.Attribute("Include") == @"Module\**\*.*");

        Assert.Equal(@"%(RecursiveDir)%(Filename)%(Extension)", (string?)moduleContent.Attribute("Link"));
    }

    [Fact]
    public void LauncherExpectsRootTccExecutableAndRootGpkPayloads()
    {
        var launcher = File.ReadAllText(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "Module",
            "client",
            "tcc-launcher.js"));

        Assert.Contains("Path.join(__dirname, \"../TCC.exe\")", launcher);
        Assert.Contains("installer.gpk(`client/gpk/${arch}/${removerGpkName}`)", launcher);
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
    public void LauncherDoesNotInstallDeadChatOrLfgRemovers()
    {
        var launcher = File.ReadAllText(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "Module",
            "client",
            "tcc-launcher.js"));

        Assert.DoesNotContain("S1UI_Chat2.gpk", launcher);
        Assert.DoesNotContain("S1UI_PartyBoard.gpk", launcher);
        Assert.DoesNotContain("S1UI_PartyBoardMemberInfo.gpk", launcher);
        Assert.DoesNotContain("ChatEnabled", launcher);
        Assert.DoesNotContain("LfgWindowSettings", launcher);
    }

    [Fact]
    public void NetworkStubDoesNotSendFakeChatOrBlockLfg()
    {
        var stub = File.ReadAllText(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "Module",
            "network",
            "tcc-stub.js"));

        Assert.DoesNotContain("S_SHOW_PARTY_MATCH_INFO", stub);
        Assert.DoesNotContain("S_PARTY_MEMBER_INFO", stub);
        Assert.DoesNotContain("S_SHOW_CANDIDATE_LIST", stub);
        Assert.DoesNotContain("S_CHAT", stub);
        Assert.DoesNotContain("S_WHISPER", stub);
        Assert.DoesNotContain("S_PRIVATE_CHAT", stub);
        Assert.DoesNotContain("notifyShowIngameChatChanged", stub);
        Assert.DoesNotContain("TccChatEnabled", stub);
        Assert.DoesNotContain("useLfg", stub);
    }

    [Fact]
    public void GlobalRpcDoesNotExposeDeadChatOrLfgPacketActions()
    {
        var rpc = File.ReadAllText(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "Module",
            "global",
            "lib",
            "rpc-handler.js"));

        Assert.DoesNotContain("chatLinkAction", rpc);
        Assert.DoesNotContain("C_APPLY_PARTY", rpc);
        Assert.DoesNotContain("C_REQUEST_PARTY_MATCH", rpc);
        Assert.DoesNotContain("C_REGISTER_PARTY_INFO", rpc);
        Assert.DoesNotContain("C_UNREGISTER_PARTY_INFO", rpc);
        Assert.DoesNotContain("C_PARTY_APPLICATION_DENIED", rpc);
        Assert.DoesNotContain("C_REQUEST_CANDIDATE_LIST", rpc);
        Assert.DoesNotContain("ShowIngameChat", rpc);
        Assert.DoesNotContain("TccChatEnabled", rpc);
        Assert.DoesNotContain("useLfg", rpc);
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
