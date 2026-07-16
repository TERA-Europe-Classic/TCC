using System.Reflection;
using System.Windows.Threading;
using System.Xml.Linq;
using TCC;
using TCC.Data.Skills;
using TCC.Settings;
using TCC.UI.Converters;
using TCC.ViewModels;
using TCC.ViewModels.ClassManagers;
using Class = TeraDataLite.Class;

namespace TCC.Tests;

public class ConfigurableClassSlotsTests
{
    private sealed class NoDefaultsLayoutViewModel : BaseClassLayoutViewModel;

    private static void EnsureAppEnvironment()
    {
        if (App.BaseDispatcher == null)
        {
            typeof(App).GetProperty(nameof(App.BaseDispatcher))!
                .SetValue(null, Dispatcher.CurrentDispatcher);
        }

        if (App.Settings == null!)
        {
            typeof(App).GetProperty(nameof(App.Settings))!
                .SetValue(null, new SettingsContainer());
        }
    }

    [Fact]
    public void ClassesWithoutDefaultSkillsStillGetConfigurableSlots()
    {
        EnsureAppEnvironment();
        var vm = new NoDefaultsLayoutViewModel();

        Assert.True(vm.HasConfigurableSkillSlots);

        for (var i = 0u; i < 4; i++)
        {
            var skill = new Skill(100 + i, Class.Lancer, $"Skill{i}", "") { IconName = $"icon{i}" };
            vm.AddExtraSkill(skill, Class.Lancer, save: false);
        }

        Assert.Equal(3, vm.ExtraSkills.Count);
    }

    [Fact]
    public void NullLayoutHasNoConfigurableSlots()
    {
        EnsureAppEnvironment();
        var vm = new NullClassLayoutViewModel();

        Assert.False(vm.HasConfigurableSkillSlots);
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(2, 0, -23)]
    [InlineData(2, 1, 23)]
    [InlineData(3, 0, -46)]
    [InlineData(3, 1, 0)]
    [InlineData(3, 2, 46)]
    public void RowProfilePlacesSlotsInCenteredHorizontalRow(int count, int index, double expectedX)
    {
        var offset = ClassSkillSlotTransformConverter.GetOffset("Row", count, index);

        Assert.Equal(expectedX, offset.X);
        Assert.Equal(0, offset.Y);
    }

    [Theory]
    [InlineData("WarriorLayout.xaml")]
    [InlineData("LancerLayout.xaml")]
    [InlineData("SlayerLayout.xaml")]
    [InlineData("BerserkerLayout.xaml")]
    [InlineData("ArcherLayout.xaml")]
    [InlineData("PriestLayout.xaml")]
    [InlineData("MysticLayout.xaml")]
    [InlineData("ReaperLayout.xaml")]
    [InlineData("GunnerLayout.xaml")]
    [InlineData("BrawlerLayout.xaml")]
    [InlineData("ValkyrieLayout.xaml")]
    public void ClassLayoutsRenderConfigurableSlots(string layoutFile)
    {
        var layout = XDocument.Load(Path.Combine(
            FindRepoRoot().FullName, "TCC.Core", "UI", "Controls", "Classes", layoutFile));

        Assert.Contains(
            layout.Descendants(),
            element => element.Name.LocalName == "ConfigurableClassSlots");
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
