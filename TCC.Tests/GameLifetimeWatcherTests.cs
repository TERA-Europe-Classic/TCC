using TCC.Utilities;

namespace TCC.Tests;

/// TCC is a game overlay — it has no purpose once the client is gone, and
/// the Classic+ launcher's teardown does not reliably reach it. The watcher
/// makes TCC close itself, independent of who started it.
public class GameLifetimeWatcherTests
{
    private static GameLifetimeWatcher Watcher(Func<bool> probe, Action onClose)
    {
        return new GameLifetimeWatcher(probe, onClose);
    }

    [Fact]
    public void DoesNotCloseBeforeTheGameHasEverAppeared()
    {
        // TCC is routinely started before the client — auto_launch fires at
        // launch time, and users open it from the tray while patching. It
        // must sit and wait, not exit immediately.
        var closed = false;
        var watcher = Watcher(() => false, () => closed = true);

        for (var i = 0; i < 10; i++) watcher.Tick();

        Assert.False(closed);
    }

    [Fact]
    public void DoesNotCloseWhileTheGameIsRunning()
    {
        var closed = false;
        var watcher = Watcher(() => true, () => closed = true);

        for (var i = 0; i < 10; i++) watcher.Tick();

        Assert.False(closed);
    }

    [Fact]
    public void ClosesOnceTheGameDisappears()
    {
        var closed = false;
        var running = false;
        var watcher = Watcher(() => running, () => closed = true);

        watcher.Tick();          // before launch
        running = true;
        watcher.Tick();          // client up
        Assert.False(closed);

        running = false;
        watcher.Tick();          // client closed

        Assert.True(closed);
    }

    [Fact]
    public void RequestsCloseOnlyOnce()
    {
        // RequestClose runs TCC's full shutdown path; firing it repeatedly
        // while the app winds down would re-enter that path.
        var closeCount = 0;
        var running = true;
        var watcher = Watcher(() => running, () => closeCount++);

        watcher.Tick();
        running = false;
        for (var i = 0; i < 5; i++) watcher.Tick();

        Assert.Equal(1, closeCount);
    }

    [Fact]
    public void SurvivesAProbeThatThrows()
    {
        // Process enumeration can throw transiently (access denied on a
        // process exiting mid-enumeration). A throwing probe must not kill
        // the timer thread or trigger a spurious close.
        var closed = false;
        var watcher = Watcher(() => throw new InvalidOperationException("probe blew up"),
                              () => closed = true);

        watcher.Tick();

        Assert.False(closed);
    }
}
