namespace TCC.Tests;

public class ClassWindowEffectWiringTests
{
    private static string ReadCoreFile(params string[] parts)
    {
        return File.ReadAllText(Path.Combine(
            [FindRepoRoot().FullName, "TCC.Core", .. parts]));
    }

    [Fact]
    public void GameRoutesSelfAbnormalitiesToClassWindowByIconFamily()
    {
        var source = ReadCoreFile("Game.cs");

        Assert.Contains("RouteClassWindowEffectBegin(", source);
        Assert.Contains("RouteClassWindowEffectRefresh(", source);
        Assert.Contains("RouteClassWindowEffectEnd(", source);
        Assert.Contains("StartEffectByIconName(", source);
        Assert.Contains("RefreshEffectByIconName(", source);
        Assert.Contains("StopEffectByIconName(", source);
    }

    [Theory]
    [InlineData("LancerLayoutViewModel.cs", "GuardianShout", "AdrenalineRush")]
    [InlineData("WarriorLayoutViewModel.cs", "DeadlyGamble", "AdrenalineRush", "Swift")]
    [InlineData("ArcherLayoutViewModel.cs", "Windsong")]
    [InlineData("MysticLayoutViewModel.cs", "Vow", "ThrallOfVengeance", "ThrallOfWrath")]
    [InlineData("GunnerLayoutViewModel.cs", "ModularSystem")]
    [InlineData("ValkyrieLayoutViewModel.cs", "Ragnarok", "Godsfall")]
    [InlineData("BrawlerLayoutViewModel.cs", "GrowingFury")]
    [InlineData("BerserkerLayoutViewModel.cs", "FieryRage", "Bloodlust", "Unleash")]
    [InlineData("SorcererLayoutViewModel.cs", "ManaBoost", "BurstOfCelerity")]
    [InlineData("PriestLayoutViewModel.cs", "Grace", "EnergyStars", "DivineCharge")]
    [InlineData("SlayerLayoutViewModel.cs", "InColdBlood")]
    [InlineData("ReaperLayoutViewModel.cs", "ShadowReaping", "ShroudedEscape")]
    public void ClassViewModelsExposeNativeEffectTilesForIconRouting(string file, params string[] skills)
    {
        var source = ReadCoreFile("ViewModels", "ClassManagers", file);

        Assert.Contains("SpecialEffectSkills", source);
        foreach (var skill in skills)
        {
            Assert.Contains(skill, source);
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
}
