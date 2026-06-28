using TCC.Update;

namespace TCC.Tests;

public class IconUpdateSourceTests
{
    [Fact]
    public void IconUpdaterUsesClassicPlusIconRepository()
    {
        Assert.Equal(
            "https://github.com/TERA-Europe-Classic/tera-used-icons/archive/master.zip",
            IconUpdateSource.ArchiveUrl);
        Assert.Equal(
            "https://raw.githubusercontent.com/TERA-Europe-Classic/tera-used-icons/master/hashes.json",
            IconUpdateSource.HashesUrl);
        Assert.Equal(
            "https://raw.githubusercontent.com/TERA-Europe-Classic/tera-used-icons/master/icon_equipments/ring_04_tex.png",
            IconUpdateSource.GetIconUrl("icon_equipments", "ring_04_tex.png"));
    }

    [Fact]
    public void MissingLocalIconNeedsUpdateWithoutHashingMissingFile()
    {
        var missingPath = Path.Combine(
            Path.GetTempPath(),
            "tcc-tests",
            Guid.NewGuid().ToString("N"),
            "ring_04_tex.png");

        Assert.True(IconUpdateSource.NeedsUpdate(missingPath, "irrelevant"));
    }
}
