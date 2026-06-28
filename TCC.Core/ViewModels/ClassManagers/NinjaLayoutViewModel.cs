using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TCC.Data;
using TCC.Data.Skills;

namespace TCC.ViewModels.ClassManagers;

public class NinjaLayoutViewModel : BaseClassLayoutViewModel
{
    private const uint InnerHarmonySkillId = 230100;
    private bool _focusOn;

    protected override IReadOnlyList<uint> DefaultClassSkillIds { get; } = [80200, 150700, 230100];

    public bool FocusOn
    {
        get => _focusOn;
        set => RaiseAndSetIfChanged(value, ref _focusOn);
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
        FindConfiguredSkill(InnerHarmonySkillId)?.Start(duration);
    }

    public void RefreshInnerHarmonyEffect(ulong duration)
    {
        FindConfiguredSkill(InnerHarmonySkillId)?.Refresh(duration, CooldownMode.Normal);
    }

    public void StopInnerHarmonyEffect()
    {
        FindConfiguredSkill(InnerHarmonySkillId)?.Stop();
    }

    private void FlashOnMaxSt(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(StaminaTracker.Maxed)) return;
        foreach (var cooldown in ExtraSkills.ToSyncList())
        {
            cooldown.FlashOnAvailable = StaminaTracker.Maxed;
        }
    }

    private Cooldown? FindConfiguredSkill(uint skillId)
    {
        return ExtraSkills.ToSyncList().FirstOrDefault(cooldown => cooldown.Skill.Id == skillId);
    }
}
