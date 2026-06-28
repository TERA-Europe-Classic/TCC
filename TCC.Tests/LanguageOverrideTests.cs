using TCC.Utils;

namespace TCC.Tests;

public class LanguageOverrideTests
{
    [Theory]
    [InlineData(LanguageOverride.None, true)]
    [InlineData(LanguageOverride.EU_EN, true)]
    [InlineData(LanguageOverride.EU_FR, true)]
    [InlineData(LanguageOverride.EU_GER, true)]
    [InlineData(LanguageOverride.RU, true)]
    [InlineData(LanguageOverride.NA, false)]
    [InlineData(LanguageOverride.KR, false)]
    [InlineData(LanguageOverride.JP, false)]
    [InlineData(LanguageOverride.TW, false)]
    [InlineData(LanguageOverride.SE, false)]
    [InlineData(LanguageOverride.THA, false)]
    public void IsClassicPlusSupportedOnlyAllowsGeneratedLanguages(LanguageOverride value, bool expected)
    {
        Assert.Equal(expected, value.IsClassicPlusSupported());
    }

    [Fact]
    public void SanitizeForClassicPlusDropsUnsupportedOverrides()
    {
        Assert.Equal(LanguageOverride.None, LanguageOverride.NA.SanitizeForClassicPlus());
    }
}
