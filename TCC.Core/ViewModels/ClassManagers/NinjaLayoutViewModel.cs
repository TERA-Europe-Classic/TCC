using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TCC.Data;
using TCC.Data.Skills;
using TCC.ViewModels;

namespace TCC.ViewModels.ClassManagers;

public class NinjaLayoutViewModel : BaseClassLayoutViewModel
{
    private const uint InnerHarmonySkillId = 230100;
    private bool _focusOn;
    private int _focusStacks;

    protected override IReadOnlyList<uint> DefaultClassSkillIds { get; } = [80200, 150700, 230100];

    public bool FocusOn
    {
        get => _focusOn;
        set => RaiseAndSetIfChanged(value, ref _focusOn);
    }

    public int FocusStacks
    {
        get => _focusStacks;
        set => RaiseAndSetIfChanged(value, ref _focusStacks);
    }

    public NinjaLayoutViewModel()
    {
        StaminaTracker.PropertyChanged += FlashOnMaxSt;
    }

    public override void Dispose()
    {
        StaminaTracker.PropertyChanged -= FlashOnMaxSt;
        base.Dispose();
    }

    protected override void ConfigureExtraSkill(Cooldown cooldown)
    {
        cooldown.CanFlash = true;
        cooldown.FlashOnAvailable = StaminaTracker.Maxed;
    }

    public void StartInnerHarmonyEffect(ulong duration)
    {
        StartConfiguredSkillEffect(InnerHarmonySkillId, duration);
    }

    public void RefreshInnerHarmonyEffect(ulong duration)
    {
        RefreshConfiguredSkillEffect(InnerHarmonySkillId, duration);
    }

    public void StopInnerHarmonyEffect()
    {
        StopConfiguredSkillEffect(InnerHarmonySkillId);
    }

    private void FlashOnMaxSt(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(StaminaTracker.Maxed)) return;
        foreach (var skill in ExtraSkills.ToSyncList())
        {
            skill.Cooldown.FlashOnAvailable = StaminaTracker.Maxed;
        }
    }
}
