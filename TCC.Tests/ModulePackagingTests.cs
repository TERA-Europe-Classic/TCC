using System.Xml.Linq;

namespace TCC.Tests;

public class ModulePackagingTests
{
    [Fact]
    public void ModuleFilesArePublishedAtToolboxModuleRoot()
    {
        var project = XDocument.Load(Path.Combine(FindRepoRoot().FullName, "TCC.Core", "TCC.Core.csproj"));
        var moduleContent = project
            .Descendants("Content")
            .Single(element => (string?)element.Attribute("Include") == @"Module\**\*.*");

        Assert.Equal(@"%(RecursiveDir)%(Filename)%(Extension)", (string?)moduleContent.Attribute("Link"));
    }

    [Fact]
    public void LauncherExpectsRootTccExecutableAndRootGpkPayloads()
    {
        var launcher = File.ReadAllText(Path.Combine(
            FindRepoRoot().FullName,
            "TCC.Core",
            "Module",
            "client",
            "tcc-launcher.js"));

        Assert.Contains("Path.join(__dirname, \"../TCC.exe\")", launcher);
        Assert.Contains("installer.gpk(`client/gpk/${arch}/${removerGpkName}`)", launcher);
    }

    private static DirectoryInfo FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "TCC.sln")))
        {
            current = current.Parent;
        }

        return current ?? throw new DirectoryNotFoundException("Could not find TCC.sln from test output.");
    }
}
