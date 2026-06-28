using System.Xml.Linq;
using TCC.UI.Converters;

namespace TCC.Tests;

public class ClassWindowExtraSkillsTests
{
    [Fact]
    public void ClassWindowDoesNotRenderConfigurableSkillsInSeparateRow()
    {
        var layout = XDocument.Load(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "UI",
            "Windows",
            "Widgets",
            "ClassWindow.xaml"));

        var extraSkillsItemsControl = layout
            .Descendants()
            .SingleOrDefault(element =>
                element.Name.LocalName == "ItemsControl"
                && (string?)element.Attribute("ItemsSource") == "{Binding CurrentManager.ExtraSkills}");

        Assert.Null(extraSkillsItemsControl);
    }

    [Fact]
    public void ClassWindowSkillConfigButtonIsTopLeftHoverOverlay()
    {
        var layout = XDocument.Load(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "UI",
            "Windows",
            "Widgets",
            "ClassWindow.xaml"));

        var style = layout
            .Descendants()
            .SingleOrDefault(element =>
                element.Name.LocalName == "Style"
                && (string?)element.Attribute("{http://schemas.microsoft.com/winfx/2006/xaml}Key") == "ClassSkillConfigButtonStyle");

        Assert.NotNull(style);
        Assert.Contains(
            style!.Descendants(),
            element =>
                element.Name.LocalName == "Setter"
                && (string?)element.Attribute("Property") == "Opacity"
                && (string?)element.Attribute("Value") == "0");
        Assert.Contains(
            style.Descendants(),
            element =>
                element.Name.LocalName == "Setter"
                && (string?)element.Attribute("Property") == "HorizontalAlignment"
                && (string?)element.Attribute("Value") == "Left");
        Assert.Contains(
            style.Descendants(),
            element =>
                element.Name.LocalName == "Setter"
                && (string?)element.Attribute("Property") == "VerticalAlignment"
                && (string?)element.Attribute("Value") == "Top");
        Assert.Contains(
            style.Descendants(),
            element => element.Name.LocalName == "DataTrigger"
                       && (string?)element.Attribute("Binding") == "{Binding IsMouseOver, RelativeSource={RelativeSource AncestorType=Grid}}");
    }

    [Fact]
    public void ClassWindowHasCommandToOpenExtraSkillEditor()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "ViewModels",
            "Widgets",
            "ClassWindowViewModel.cs"));

        Assert.Contains("OpenSkillConfigCommand", source);
        Assert.Contains("ClassSkillConfigWindow.Instance.ShowWindow()", source);
    }

    [Fact]
    public void NinjaNativeSkillSlotsUseConfigurableClassSkillList()
    {
        var layout = XDocument.Load(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "UI",
            "Controls",
            "Classes",
            "NinjaLayout.xaml"));

        var configurableSlots = layout
            .Descendants()
            .SingleOrDefault(element =>
                element.Name.LocalName == "ItemsControl"
                && (string?)element.Attribute("ItemsSource") == "{Binding ExtraSkills}");

        Assert.NotNull(configurableSlots);
        Assert.Contains(
            configurableSlots!.Descendants(),
            element => element.Name.LocalName == "RhombFixedSkillControl");

        var classSkillSlotBorders = layout
            .Descendants()
            .Where(element =>
                element.Name.LocalName == "Border"
                && (string?)element.Attribute("Width") == "51"
                && (string?)element.Attribute("Height") == "51")
            .ToList();

        Assert.Single(classSkillSlotBorders);
        Assert.Contains(classSkillSlotBorders[0], configurableSlots.Descendants());

        var fixedBindings = layout
            .Descendants()
            .Where(element => element.Name.LocalName is "RhombFixedSkillControl" or "RhombSkillEffectControl")
            .Select(element => (string?)element.Attribute("DataContext"))
            .Where(value => value != null)
            .ToList();

        Assert.DoesNotContain("{Binding FireAvalanche}", fixedBindings);
        Assert.DoesNotContain("{Binding BurningHeart}", fixedBindings);
        Assert.DoesNotContain("{Binding InnerHarmony}", fixedBindings);
    }

    [Fact]
    public void NinjaDefaultsToOriginalThreeClassWindowSkills()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "ViewModels",
            "ClassManagers",
            "NinjaLayoutViewModel.cs"));

        Assert.Contains("DefaultClassSkillIds", source);
        Assert.Contains("[80200, 150700, 230100]", source);
    }

    [Theory]
    [InlineData(1, 0, 0, 90)]
    [InlineData(2, 0, 45, 45)]
    [InlineData(2, 1, -45, 45)]
    [InlineData(3, 0, 45, 45)]
    [InlineData(3, 1, -45, 45)]
    [InlineData(3, 2, 0, 90)]
    public void NinjaClassSkillSlotsCompactAroundActualConfiguredSkillCount(
        int skillCount,
        int skillIndex,
        double expectedX,
        double expectedY)
    {
        var offset = ClassSkillSlotTransformConverter.GetOffset(skillCount, skillIndex);

        Assert.Equal(expectedX, offset.X);
        Assert.Equal(expectedY, offset.Y);
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
