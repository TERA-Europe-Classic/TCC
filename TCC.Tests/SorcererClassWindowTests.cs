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
    public void ViewModelPinsFusionIconsToExistingClassicPlusAssets()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "ViewModels",
            "ClassManagers",
            "SorcererLayoutViewModel.cs"));

        Assert.Contains("FusionIconName = \"icon_skills.fusion_tex\"", source);
        Assert.Contains("ElementalFusionIconName = \"icon_skills.elementfusion_tex\"", source);
        Assert.Contains("fusion.IconName = FusionIconName", source);
        Assert.Contains("fusionBoost.IconName = ElementalFusionIconName", source);
    }

    [Fact]
    public void TrackerWiresBurstOfCelerityEffectDuration()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "Data",
            "Abnormalities",
            "SorcererAbnormalityTracker.cs"));

        Assert.Contains("BurstOfCelerityId = 501200", source);
        Assert.Contains("CheckBurstOfCelerityBegin(p)", source);
        Assert.Contains("CheckBurstOfCelerityRefresh(p)", source);
        Assert.Contains("CheckBurstOfCelerityEnd(p)", source);
        Assert.Contains("vm.BurstOfCelerity.StartEffect(p.Duration)", source);
        Assert.Contains("vm.BurstOfCelerity.RefreshEffect(p.Duration)", source);
        Assert.Contains("vm.BurstOfCelerity.StopEffect()", source);
    }

    [Fact]
    public void LayoutPreservesBurstOfCelerityEffectDurationInConfigurableSlot()
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
                && (string?)element.Attribute("DataContext") == "{Binding BurstOfCelerity, RelativeSource={RelativeSource AncestorType=UserControl}}");

        Assert.NotNull(burstTile);

        Assert.Contains(
            layout.Descendants(),
            element =>
                element.Name.LocalName == "DataTrigger"
                && (string?)element.Attribute("Binding") == "{Binding Skill.Id}"
                && (string?)element.Attribute("Value") == "240100");
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
