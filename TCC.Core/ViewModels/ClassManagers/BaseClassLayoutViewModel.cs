using System;
using System.Collections.Generic;
using System.Linq;
using Nostrum.WPF.ThreadSafe;
using TCC.Data;
using TCC.Data.Skills;
using TCC.Settings;
using TeraDataLite;

namespace TCC.ViewModels.ClassManagers;

public abstract class BaseClassLayoutViewModel : ThreadSafeObservableObject, IDisposable
{
    public ThreadSafeObservableCollection<Cooldown> ExtraSkills { get; }
    protected virtual IReadOnlyList<uint> DefaultClassSkillIds => [];
    protected virtual int ClassSkillCapacity => DefaultClassSkillIds.Count;
    public bool HasConfigurableSkillSlots => ClassSkillCapacity > 0;

    protected BaseClassLayoutViewModel()
    {
        ExtraSkills = new ThreadSafeObservableCollection<Cooldown>(_dispatcher);
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
        if (ExtraSkills.ToSyncList().Any(x => x.Skill.IconName == skill.IconName)) return false;

        var cooldown = new Cooldown(skill, false, CooldownType.Skill, _dispatcher);
        ConfigureExtraSkill(cooldown);
        if (index < 0 || index > ExtraSkills.Count)
        {
            ExtraSkills.Add(cooldown);
        }
        else
        {
            ExtraSkills.Insert(index, cooldown);
        }

        if (save) SaveExtraSkills(c);
        return true;
    }

    public bool RemoveExtraSkill(Cooldown cooldown, Class c)
    {
        var target = ExtraSkills.ToSyncList().FirstOrDefault(x => x.Skill.IconName == cooldown.Skill.IconName);
        if (target == null) return false;

        ExtraSkills.Remove(target);
        target.Dispose();
        SaveExtraSkills(c);
        return true;
    }

    public bool MoveExtraSkill(Cooldown cooldown, int insertIndex, Class c)
    {
        var target = ExtraSkills.ToSyncList().FirstOrDefault(x => x.Skill.IconName == cooldown.Skill.IconName);
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
        var existing = ExtraSkills.ToSyncList().FirstOrDefault(x => x.Skill.IconName == skill.IconName);
        if (existing == null) return false;

        existing.Refresh(skill.Id, cd, CooldownMode.Normal);
        return true;
    }

    public bool ResetExtraSkill(Skill skill)
    {
        var existing = ExtraSkills.ToSyncList().FirstOrDefault(x => x.Skill.IconName == skill.IconName);
        if (existing == null) return false;

        existing.ProcReset();
        return true;
    }

    public void SaveExtraSkills(Class c)
    {
        if (c is Class.None) return;

        var data = new ClassWindowConfigData();
        foreach (var cooldown in ExtraSkills.ToSyncList())
        {
            data.SkillIds.Add(cooldown.Skill.Id);
        }

        ClassWindowConfigParser.Save(c, data);
    }

    public void ClearExtraSkills()
    {
        foreach (var cooldown in ExtraSkills.ToSyncList())
        {
            cooldown.Dispose();
        }

        ExtraSkills.Clear();
    }

    protected virtual void ConfigureExtraSkill(Cooldown cooldown)
    {
    }

    private bool StartExtraSkill(Cooldown cd)
    {
        var existing = ExtraSkills.ToSyncList().FirstOrDefault(x => x.Skill.IconName == cd.Skill.IconName);
        if (existing == null) return false;

        existing.Start(cd.Duration, cd.Mode);
        return true;
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
