using TCC.UI.Controls;

namespace TCC.Tests;

public class TrayIconLifecycleTests
{
    [Fact]
    public void CreatesInitialIconWithoutImmediatelyDisposingIt()
    {
        var created = new List<FakeTrayIcon>();

        using var lifecycle = new TrayIconLifecycle<FakeTrayIcon>(
            () =>
            {
                var icon = new FakeTrayIcon();
                created.Add(icon);
                return icon;
            },
            (icon, visible) => icon.Visible = visible);

        Assert.Single(created);
        Assert.True(created[0].Visible);
        Assert.False(created[0].Disposed);
        Assert.Same(created[0], lifecycle.Current);
    }

    [Fact]
    public void RecreateDisposesPreviousIconAndShowsReplacement()
    {
        var created = new List<FakeTrayIcon>();
        using var lifecycle = new TrayIconLifecycle<FakeTrayIcon>(
            () =>
            {
                var icon = new FakeTrayIcon();
                created.Add(icon);
                return icon;
            },
            (icon, visible) => icon.Visible = visible);

        lifecycle.Recreate();

        Assert.Equal(2, created.Count);
        Assert.False(created[0].Visible);
        Assert.True(created[0].Disposed);
        Assert.True(created[1].Visible);
        Assert.False(created[1].Disposed);
        Assert.Same(created[1], lifecycle.Current);
    }

    private sealed class FakeTrayIcon : IDisposable
    {
        public bool Visible { get; set; }
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
