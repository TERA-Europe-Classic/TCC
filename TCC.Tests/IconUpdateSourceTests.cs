using TCC.Update;

namespace TCC.Tests;

public class IconUpdateSourceTests
{
    [Fact]
    public void IconUpdaterUsesClassicPlusIconRepository()
    {
        Assert.Equal(
            "https://github.com/TERA-Europe-Classic/tera-used-icons/archive/main.zip",
            IconUpdateSource.ArchiveUrl);
        Assert.Equal(
            "https://raw.githubusercontent.com/TERA-Europe-Classic/tera-used-icons/main/hashes.json",
            IconUpdateSource.HashesUrl);
        Assert.Equal(
            "https://raw.githubusercontent.com/TERA-Europe-Classic/tera-used-icons/main/icon_equipments/ring_04_tex.png",
            IconUpdateSource.GetIconUrl("icon_equipments", "ring_04_tex.png"));
        Assert.Equal("tera-used-icons-main", IconUpdateSource.ArchiveDirectoryName);
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
