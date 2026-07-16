using System.Reflection;
using System.Windows.Threading;
using TCC;
using TCC.Data.Skills;
using TCC.Settings;
using TCC.ViewModels;
using TCC.ViewModels.ClassManagers;
using Class = TeraDataLite.Class;

namespace TCC.Tests;

public class ClassWindowEffectRoutingTests
{
    private const string GuardianShoutIcon = "icon_skills.inspiringroar_tex";
    private const string AdrenalineRushIcon = "icon_skills.fightingwill_tex";

    private sealed class TestLayoutViewModel : BaseClassLayoutViewModel
    {
        public SkillWithEffect Native { get; }

        public TestLayoutViewModel(SkillWithEffect native)
        {
            Native = native;
        }

        protected override IEnumerable<SkillWithEffect> SpecialEffectSkills => [Native];
    }

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

    private static SkillWithEffect MakeSkill(uint id, string icon)
    {
        var skill = new Skill(id, Class.Lancer, "Test", "") { IconName = icon };
        return new SkillWithEffect(Dispatcher.CurrentDispatcher, skill);
    }

    [Fact]
    public void StartEffectByIconName_StartsMatchingNativeSkillEffect()
    {
        EnsureAppEnvironment();
        var native = MakeSkill(70300, GuardianShoutIcon);
        var vm = new TestLayoutViewModel(native);

        var started = vm.StartEffectByIconName(GuardianShoutIcon, 30000);

        Assert.True(started);
        Assert.True(native.Effect.Seconds > 0);
    }

    [Fact]
    public void StartEffectByIconName_IgnoresNonMatchingIcon()
    {
        EnsureAppEnvironment();
        var native = MakeSkill(70300, GuardianShoutIcon);
        var vm = new TestLayoutViewModel(native);

        var started = vm.StartEffectByIconName(AdrenalineRushIcon, 30000);

        Assert.False(started);
        Assert.Equal(0, native.Effect.Seconds);
    }

    [Fact]
    public void StopEffectByIconName_StopsRunningEffect()
    {
        EnsureAppEnvironment();
        var native = MakeSkill(70300, GuardianShoutIcon);
        var vm = new TestLayoutViewModel(native);
        vm.StartEffectByIconName(GuardianShoutIcon, 30000);

        var stopped = vm.StopEffectByIconName(GuardianShoutIcon);

        Assert.True(stopped);
        Assert.Equal(0, native.Effect.Seconds);
    }

    [Fact]
    public void RefreshEffectByIconName_UpdatesRunningEffectDuration()
    {
        EnsureAppEnvironment();
        var native = MakeSkill(70300, GuardianShoutIcon);
        var vm = new TestLayoutViewModel(native);
        vm.StartEffectByIconName(GuardianShoutIcon, 10000);

        var refreshed = vm.RefreshEffectByIconName(GuardianShoutIcon, 40000);

        Assert.True(refreshed);
        Assert.True(native.Effect.Seconds > 30);
    }

    [Fact]
    public void StartEffectByIconName_MatchesBlankIconNever()
    {
        EnsureAppEnvironment();
        var native = MakeSkill(70300, "");
        var vm = new TestLayoutViewModel(native);

        var started = vm.StartEffectByIconName("", 30000);

        Assert.False(started);
    }
}
