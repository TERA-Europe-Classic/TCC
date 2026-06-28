using TCC.Settings;
using TeraDataLite;

namespace TCC.Tests;

public class ClassWindowConfigParserTests
{
    [Fact]
    public void DefaultsToEmptyExtraSkillList()
    {
        using var temp = new TempResourcesRoot();

        var data = new ClassWindowConfigParser(Class.Sorcerer, temp.Path).Data;

        Assert.Empty(data.SkillIds);
    }

    [Fact]
    public void SavesAndLoadsPerClassExtraSkillIds()
    {
        using var temp = new TempResourcesRoot();
        var data = new ClassWindowConfigData();
        data.SkillIds.Add(240100);
        data.SkillIds.Add(340200);

        ClassWindowConfigParser.Save(Class.Sorcerer, data, temp.Path);

        var loaded = new ClassWindowConfigParser(Class.Sorcerer, temp.Path).Data;

        Assert.Equal([240100u, 340200u], loaded.SkillIds);
        Assert.False(File.Exists(Path.Combine(temp.Path, "config", "class-window-skills", "lancer-skills.json")));
    }

    private sealed class TempResourcesRoot : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "tcc-class-window-config-tests",
            Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
