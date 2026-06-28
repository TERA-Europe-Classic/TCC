using System.Xml.Linq;

namespace TCC.Tests;

public class SorcererClassWindowTests
{
    [Fact]
    public void ViewModelWiresBurstOfCelerityCooldown()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "ViewModels",
            "ClassManagers",
            "SorcererLayoutViewModel.cs"));

        Assert.Contains("public SkillWithEffect BurstOfCelerity", source);
        Assert.Contains("TryGetSkill(240100, Class.Sorcerer", source);
        Assert.Contains("BurstOfCelerity = new SkillWithEffect", source);
        Assert.Contains("BurstOfCelerity.StartCooldown(sk.Duration)", source);
        Assert.Contains("BurstOfCelerity.Dispose()", source);
    }

    [Fact]
    public void LayoutShowsBurstOfCeleritySkillEffectTile()
    {
        var layout = XDocument.Load(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "UI",
            "Controls",
            "Classes",
            "SorcererLayout.xaml"));

        var burstTile = layout
            .Descendants()
            .SingleOrDefault(element =>
                element.Name.LocalName == "RhombSkillEffectControl"
                && (string?)element.Attribute("DataContext") == "{Binding BurstOfCelerity}");

        Assert.NotNull(burstTile);
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
