namespace TCC.Tests;

public class CooldownAliasTests
{
    [Fact]
    public void CooldownRefreshDoesNotRejectClassicPlusChildSkillAliases()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "Data",
            "Skills",
            "Cooldown.cs"));

        Assert.DoesNotContain("Skill.Id % 10 == 0 && id % 10 != 0", source);
    }

    [Fact]
    public void CooldownWindowChangeDoesNotRejectClassicPlusChildSkillAliases()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "ViewModels",
            "Widgets",
            "CooldownWindowViewModel.cs"));

        Assert.DoesNotContain("skill.Id % 10 != 0", source);
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
