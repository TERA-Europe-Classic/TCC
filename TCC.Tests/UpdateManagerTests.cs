using TCC.Update;

namespace TCC.Tests;

public class UpdateManagerTests
{
    [Fact]
    public void GetAppVersionInfoUrlUsesClassicPlusRepository()
    {
        var url = UpdateManager.GetAppVersionInfoUrl();

        Assert.Equal("https://raw.githubusercontent.com/TERA-Europe-Classic/TCC/main/version", url);
    }

    [Fact]
    public void ForceBetaVersionInfoUsesClassicPlusRepository()
    {
        var url = UpdateManager.GetAppVersionInfoUrl(forceBeta: true);

        Assert.Equal("https://raw.githubusercontent.com/TERA-Europe-Classic/TCC/main/version", url);
    }

    [Theory]
    [InlineData("2.0.8-classicplus", 2, 0, 8)]
    [InlineData("2.0.8+build.1", 2, 0, 8)]
    [InlineData("2.0.8.1", 2, 0, 8, 1)]
    public void ParseAppVersionNumberIgnoresReleaseLabels(
        string versionNumber,
        int major,
        int minor,
        int build,
        int revision = -1)
    {
        var version = UpdateManager.ParseAppVersionNumber(versionNumber);

        var expected = revision >= 0
            ? new Version(major, minor, build, revision)
            : new Version(major, minor, build);

        Assert.Equal(expected, version);
    }

    [Fact]
    public void GetDatabaseUpdateUrlUsesClassicPlusDataRepository()
    {
        var url = UpdateManager.GetDatabaseUpdateUrl(@"items\items-EU-EN.tsv");

        Assert.Equal(
            "https://raw.githubusercontent.com/TERA-Europe-Classic/TCC-ClassicPlus-Data/main/items/items-EU-EN.tsv",
            url);
    }
}
