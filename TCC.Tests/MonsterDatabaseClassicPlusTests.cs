namespace TCC.Tests;

public class MonsterDatabaseClassicPlusTests
{
    [Fact]
    public void MonsterDatabaseLoadsClassicPlusEnrageHp()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "Data",
            "Databases",
            "MonsterDatabase.cs"));

        Assert.Contains("enrageHp", source);
        Assert.Contains("EnrageHP", source);
    }

    [Fact]
    public void NpcDefaultEnragePatternUsesMonsterEnrageHp()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "Data",
            "Npc",
            "Npc.cs"));

        Assert.Contains("monster.EnrageHP > 0", source);
        Assert.Contains("new EnragePattern(monster.MaxHP, monster.EnrageHP, 36)", source);
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
