using System;
using System.Diagnostics;
using TCC.Utils;

namespace TCC.Utilities;

/// Closes TCC once the game client it was opened for is gone.
///
/// TCC is an overlay with no standalone purpose, so it should not outlive
/// the client. The Classic+ launcher does try to tear it down, but that
/// path depends on the launcher still running and on its own view of the
/// game lifecycle; when it does not fire, TCC lingers until the user kills
/// it by hand. Watching the process directly is independent of who started
/// TCC and of whether the launcher is alive at all.
///
/// Deliberately keyed on the process, not on the packet connection: the
/// sniffer drops on every server restart, and TCC must survive those.
public class GameLifetimeWatcher
{
    private const string GameProcessName = "TERA";

    private readonly Func<bool> _isGameRunning;
    private readonly Action _requestClose;

    private bool _gameSeen;
    private bool _closeRequested;

    public GameLifetimeWatcher(Func<bool> isGameRunning, Action requestClose)
    {
        _isGameRunning = isGameRunning;
        _requestClose = requestClose;
    }

    public GameLifetimeWatcher(Action requestClose)
        : this(IsGameProcessRunning, requestClose)
    {
    }

    public void Tick()
    {
        bool running;
        try
        {
            running = _isGameRunning();
        }
        catch (Exception ex)
        {
            // A process snapshot can fail transiently. Treat it as "no
            // information" rather than "the game is gone" — closing TCC on a
            // failed probe would be worse than waiting for the next tick.
            Log.F($"GameLifetimeWatcher: game probe failed, skipping tick. {ex.Message}");
            return;
        }

        if (running)
        {
            _gameSeen = true;
            return;
        }

        // Never seen the client: TCC was started ahead of it (auto-launch,
        // or the user opened it while patching). Wait.
        if (!_gameSeen || _closeRequested) return;

        _closeRequested = true;
        _requestClose();
    }

    private static bool IsGameProcessRunning()
    {
        return Process.GetProcessesByName(GameProcessName).Length > 0;
    }
}
