using TCC.Utilities;

namespace TCC.Tests;

/// TCC is a game overlay — it has no purpose once the client is gone.
/// The watcher subscribes to the client process's exit signal rather than
/// polling for it, so it costs nothing while the game is running.
public class GameLifetimeWatcherTests
{
    /// Stand-in for a live client process.
    private sealed class FakeExitSignal : IGameExitSignal
    {
        public event Action? Exited;
        public int SubscriberCount => Exited?.GetInvocationList().Length ?? 0;
        public void FireExit() => Exited?.Invoke();
    }

    [Fact]
    public void DoesNotCloseWhenTheGameIsNotRunning()
    {
        // TCC is routinely started before the client — auto_launch fires at
        // launch time, and users open it from the tray while patching.
        var closed = false;
        var watcher = new GameLifetimeWatcher(() => null, () => closed = true);

        watcher.TryAttach();

        Assert.False(closed);
        Assert.False(watcher.IsAttached);
    }

    [Fact]
    public void ClosesWhenTheAttachedClientExits()
    {
        var closed = false;
        var signal = new FakeExitSignal();
        var watcher = new GameLifetimeWatcher(() => signal, () => closed = true);

        watcher.TryAttach();
        Assert.True(watcher.IsAttached);
        Assert.False(closed);

        signal.FireExit();

        Assert.True(closed);
    }

    [Fact]
    public void AttachesOnlyOnce()
    {
        // TryAttach is called at startup and again on every new game
        // connection — a server restart reconnects. Re-subscribing each
        // time would fire RequestClose once per subscription.
        var signal = new FakeExitSignal();
        var watcher = new GameLifetimeWatcher(() => signal, () => { });

        watcher.TryAttach();
        watcher.TryAttach();
        watcher.TryAttach();

        Assert.Equal(1, signal.SubscriberCount);
    }

    [Fact]
    public void RequestsCloseOnlyOnce()
    {
        // RequestClose runs TCC's full shutdown path; re-entering it while
        // the app winds down would tear down disposed state twice.
        var closeCount = 0;
        var signal = new FakeExitSignal();
        var watcher = new GameLifetimeWatcher(() => signal, () => closeCount++);

        watcher.TryAttach();
        signal.FireExit();
        signal.FireExit();

        Assert.Equal(1, closeCount);
    }

    [Fact]
    public void SurvivesAnAttachThatThrows()
    {
        // Opening a process can throw transiently (it exits between
        // enumeration and handle open). That must not kill the caller or
        // count as "the game is gone".
        var closed = false;
        var watcher = new GameLifetimeWatcher(
            () => throw new InvalidOperationException("open blew up"),
            () => closed = true);

        watcher.TryAttach();

        Assert.False(closed);
        Assert.False(watcher.IsAttached);
    }
}
