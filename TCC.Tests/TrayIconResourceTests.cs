using TCC.UI.Controls;

namespace TCC.Tests;

public class TrayIconResourceTests
{
    [Theory]
    [InlineData("resources/tcc_off.ico")]
    [InlineData("resources/tcc_on.ico")]
    public void TrayIconsAreEmbeddedResources(string resourcePath)
    {
        using var icon = TrayIconResources.Load(resourcePath);

        Assert.NotNull(icon);
        Assert.False(icon.Handle == IntPtr.Zero);
    }
}
