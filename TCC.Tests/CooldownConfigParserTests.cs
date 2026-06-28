using TCC.Data;
using TCC.Settings;
using TeraDataLite;

namespace TCC.Tests;

public class CooldownConfigParserTests
{
    [Fact]
    public void SorcererDefaultFixedHudDoesNotIncludeBurstOfCelerity()
    {
        var data = new CooldownConfigParser(Class.Sorcerer).Data;

        Assert.DoesNotContain(data.Main, IsBurstOfCelerity);
        Assert.DoesNotContain(data.Secondary, IsBurstOfCelerity);
        Assert.DoesNotContain(data.Hidden, IsBurstOfCelerity);
    }

    private static bool IsBurstOfCelerity(CooldownData cooldown)
        => cooldown.Id == 240100 && cooldown.Type == CooldownType.Skill;
}
