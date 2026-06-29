using System.Reflection;
using TCC.Data.Databases;
using TCC.Data.Skills;
using TeraDataLite;

namespace TCC.Tests;

public class SkillsDatabaseTests
{
    [Fact]
    public void SorcererBurstOfCelerityIsSelectable()
    {
        var burstOfCelerity = new Skill(240100, Class.Sorcerer, "Burst of Celerity", string.Empty)
        {
            IconName = "icon_skills.contractofquickness_tex"
        };

        Assert.False(IsIgnoredSkill(burstOfCelerity));
    }

    private static bool IsIgnoredSkill(Skill skill)
    {
        return (bool)typeof(SkillsDatabase)
            .GetMethod("IsIgnoredSkill", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [skill])!;
    }
}
