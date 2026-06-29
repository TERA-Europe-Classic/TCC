using System;
using System.Collections.Generic;
using System.Linq;
using Nostrum.WPF.ThreadSafe;
using TCC.Data;
using TCC.Data.Skills;
using TCC.Settings;
using TCC.ViewModels;
using TeraDataLite;

namespace TCC.ViewModels.ClassManagers;

public abstract class BaseClassLayoutViewModel : ThreadSafeObservableObject, IDisposable
{
    public ThreadSafeObservableCollection<SkillWithEffect> ExtraSkills { get; }
    protected virtual IReadOnlyList<uint> DefaultClassSkillIds => [];
    protected virtual int ClassSkillCapacity => DefaultClassSkillIds.Count;
    public bool HasConfigurableSkillSlots => ClassSkillCapacity > 0;

    protected BaseClassLayoutViewModel()
    {
        ExtraSkills = new ThreadSafeObservableCollection<SkillWithEffect>(_dispatcher);
    }

    public bool StartSpecialSkill(Cooldown cd)
    {
        var ret = StartSpecialSkillImpl(cd);
        ret = StartExtraSkill(cd) || ret;
        cd.Dispose();
        return ret;
    }

    protected virtual bool StartSpecialSkillImpl(Cooldown cd)
    {
        return false;
    }

    public virtual bool ChangeSpecialSkill(Skill skill, uint cd)
    {
        return false;
    }

    public virtual bool ResetSpecialSkill(Skill skill)
    {
        return false;
    }

    public void LoadExtraSkills(Class c)
    {
        ClearExtraSkills();
        if (c is Class.None || Game.DB == null) return;

        var parser = new ClassWindowConfigParser(c);
        var skillIds = parser.Exists ? parser.Data.SkillIds : DefaultClassSkillIds;
        foreach (var skillId in skillIds.Distinct().Take(ClassSkillCapacity))
        {
            if (Game.DB.SkillsDatabase.TryGetSkill(skillId, c, out var skill)
             || Game.DB.SkillsDatabase.TryGetSkill(skillId, Class.Common, out skill))
            {
                AddExtraSkill(skill, c, save: false);
            }
        }
    }

    public bool AddExtraSkill(Skill skill, Class c, int index = -1, bool save = true)
    {
        if (skill.Id == 0 || skill.Class is Class.None) return false;
        if (ClassSkillCapacity == 0 || ExtraSkills.Count >= ClassSkillCapacity) return false;
        if (ExtraSkills.ToSyncList().Any(x => IsSameSkill(x.Cooldown.Skill, skill))) return false;

        var classSkill = new SkillWithEffect(_dispatcher, skill);
        ConfigureExtraSkill(classSkill.Cooldown);
        if (index < 0 || index > ExtraSkills.Count)
        {
            ExtraSkills.Add(classSkill);
        }
        else
        {
            ExtraSkills.Insert(index, classSkill);
        }

        if (save) SaveExtraSkills(c);
        return true;
    }

    public bool RemoveExtraSkill(SkillWithEffect skill, Class c)
    {
        var target = ExtraSkills.ToSyncList().FirstOrDefault(x => IsSameSkill(x.Cooldown.Skill, skill.Cooldown.Skill));
        if (target == null) return false;

        ExtraSkills.Remove(target);
        target.Dispose();
        SaveExtraSkills(c);
        return true;
    }

    public bool MoveExtraSkill(SkillWithEffect skill, int insertIndex, Class c)
    {
        var target = ExtraSkills.ToSyncList().FirstOrDefault(x => IsSameSkill(x.Cooldown.Skill, skill.Cooldown.Skill));
        if (target == null) return false;

        var currentIndex = ExtraSkills.IndexOf(target);
        if (currentIndex < 0) return false;

        var targetIndex = Math.Clamp(insertIndex, 0, ExtraSkills.Count);
        if (currentIndex < targetIndex) targetIndex--;
        if (currentIndex == targetIndex) return false;

        ExtraSkills.RemoveAt(currentIndex);
        ExtraSkills.Insert(Math.Min(targetIndex, ExtraSkills.Count), target);
        SaveExtraSkills(c);
        return true;
    }

    public bool ChangeExtraSkill(Skill skill, uint cd)
    {
        var existing = ExtraSkills.ToSyncList().FirstOrDefault(x => IsSameSkill(x.Cooldown.Skill, skill));
        if (existing == null) return false;

        existing.Cooldown.Refresh(skill.Id, cd, CooldownMode.Normal);
        return true;
    }

    public bool ResetExtraSkill(Skill skill)
    {
        var existing = ExtraSkills.ToSyncList().FirstOrDefault(x => IsSameSkill(x.Cooldown.Skill, skill));
        if (existing == null) return false;

        existing.Cooldown.ProcReset();
        return true;
    }

    public void SaveExtraSkills(Class c)
    {
        if (c is Class.None) return;

        var data = new ClassWindowConfigData();
        foreach (var skill in ExtraSkills.ToSyncList())
        {
            data.SkillIds.Add(skill.Cooldown.Skill.Id);
        }

        ClassWindowConfigParser.Save(c, data);
    }

    public void ClearExtraSkills()
    {
        foreach (var skill in ExtraSkills.ToSyncList())
        {
            skill.Dispose();
        }

        ExtraSkills.Clear();
    }

    protected virtual void ConfigureExtraSkill(Cooldown cooldown)
    {
    }

    public void StartSkillEffect(SkillWithEffect skill, ulong duration)
    {
        skill.StartEffect(duration);
        StartConfiguredSkillEffect(skill.Cooldown.Skill, duration);
    }

    public void RefreshSkillEffect(SkillWithEffect skill, ulong duration)
    {
        skill.RefreshEffect(duration);
        RefreshConfiguredSkillEffect(skill.Cooldown.Skill, duration);
    }

    public void StopSkillEffect(SkillWithEffect skill)
    {
        skill.StopEffect();
        StopConfiguredSkillEffect(skill.Cooldown.Skill);
    }

    protected void StartConfiguredSkillEffect(uint skillId, ulong duration)
    {
        FindConfiguredSkill(skillId)?.StartEffect(duration);
    }

    protected void RefreshConfiguredSkillEffect(uint skillId, ulong duration)
    {
        FindConfiguredSkill(skillId)?.RefreshEffect(duration);
    }

    protected void StopConfiguredSkillEffect(uint skillId)
    {
        FindConfiguredSkill(skillId)?.StopEffect();
    }

    private bool StartExtraSkill(Cooldown cd)
    {
        var existing = ExtraSkills.ToSyncList().FirstOrDefault(x => IsSameSkill(x.Cooldown.Skill, cd.Skill));
        if (existing == null) return false;

        existing.Cooldown.Start(cd.Duration, cd.Mode);
        return true;
    }

    private void StartConfiguredSkillEffect(Skill skill, ulong duration)
    {
        FindConfiguredSkill(skill)?.StartEffect(duration);
    }

    private void RefreshConfiguredSkillEffect(Skill skill, ulong duration)
    {
        FindConfiguredSkill(skill)?.RefreshEffect(duration);
    }

    private void StopConfiguredSkillEffect(Skill skill)
    {
        FindConfiguredSkill(skill)?.StopEffect();
    }

    private SkillWithEffect? FindConfiguredSkill(uint skillId)
    {
        return ExtraSkills.ToSyncList().FirstOrDefault(skill => skill.Cooldown.Skill.Id == skillId);
    }

    private SkillWithEffect? FindConfiguredSkill(Skill skill)
    {
        return ExtraSkills.ToSyncList().FirstOrDefault(extraSkill => IsSameSkill(extraSkill.Cooldown.Skill, skill));
    }

    private static bool IsSameSkill(Skill left, Skill right)
    {
        if (left.Id != 0 && right.Id != 0 && left.Id == right.Id) return true;
        return !string.IsNullOrWhiteSpace(left.IconName) && left.IconName == right.IconName;
    }

    public StatTracker StaminaTracker { get; set; } = new();

    public void SetMaxST(int v)
    {
        if (!App.Settings.ClassWindowSettings.Enabled) return;
        StaminaTracker.Max = v;
    }

    public void SetST(int currentStamina)
    {
        if (!App.Settings.ClassWindowSettings.Enabled) return;
        StaminaTracker.Val = currentStamina;
    }

    public virtual void Dispose()
    {
        ClearExtraSkills();
    }
}
