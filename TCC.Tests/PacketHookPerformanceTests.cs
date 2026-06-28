using System.Text.RegularExpressions;

namespace TCC.Tests;

public class PacketHookPerformanceTests
{
    [Fact]
    public void GlobalGameHooksHighFrequencyPacketsOnlyOncePerHandler()
    {
        var game = File.ReadAllText(Path.Combine(FindRepoRoot().FullName, "TCC.Core", "Game.cs"));

        Assert.Single(Regex.Matches(game, @"Hook<S_CREATURE_CHANGE_HP>\(OnCreatureChangeHp\)"));
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
