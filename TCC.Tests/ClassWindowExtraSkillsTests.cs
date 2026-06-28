using System.Xml.Linq;

namespace TCC.Tests;

public class ClassWindowExtraSkillsTests
{
    [Fact]
    public void ClassWindowRendersConfigurableExtraSkillRow()
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

        Assert.NotNull(extraSkillsItemsControl);
        Assert.Contains(
            extraSkillsItemsControl!.Descendants(),
            element => element.Name.LocalName == "RhombFixedSkillControl");
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
