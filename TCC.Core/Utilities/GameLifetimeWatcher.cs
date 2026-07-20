using System;
using System.Diagnostics;
using System.Linq;
using TCC.Utils;

namespace TCC.Utilities;

/// A signal that fires once when the game client process ends.
public interface IGameExitSignal
{
    event Action Exited;
}

/// Closes TCC once the game client it was opened for is gone.
///
/// TCC is an overlay with no standalone purpose, so it should not outlive
/// the client. The Classic+ launcher also tries to tear it down, but that
/// only works while the launcher is running and only when its own view of
/// the game lifecycle says the last client is gone.
///
/// This costs nothing while the game runs: it subscribes to the process's
/// exit signal — a kernel-signalled handle — rather than polling. Attach
/// is attempted at startup and on each new game connection; both are
/// one-shot and idempotent.
///
/// Deliberately keyed on the process, not on the packet connection: the
/// sniffer drops on every server restart, and TCC must survive those.
public class GameLifetimeWatcher
{
    private const string GameProcessName = "TERA";

    private readonly Func<IGameExitSignal?> _openGameExitSignal;
    private readonly Action _requestClose;
    private readonly object _gate = new();

    private bool _closeRequested;

    public bool IsAttached { get; private set; }

    public GameLifetimeWatcher(Func<IGameExitSignal?> openGameExitSignal, Action requestClose)
    {
        _openGameExitSignal = openGameExitSignal;
        _requestClose = requestClose;
    }

    public GameLifetimeWatcher(Action requestClose)
        : this(OpenGameProcessExitSignal, requestClose)
    {
    }

    /// Subscribes to the client's exit signal if it is running and we are
    /// not already subscribed. Safe to call repeatedly.
    public void TryAttach()
    {
        lock (_gate)
        {
            if (IsAttached) return;

            IGameExitSignal? signal;
            try
            {
                signal = _openGameExitSignal();
            }
            catch (Exception ex)
            {
                // The process can exit between enumeration and handle open.
                // Treat as "not attached yet" and retry on the next
                // connection rather than assuming the game is gone.
                Log.F($"GameLifetimeWatcher: could not attach to {GameProcessName}. {ex.Message}");
                return;
            }

            if (signal == null) return;

            signal.Exited += HandleGameExited;
            IsAttached = true;
        }
    }

    private void HandleGameExited()
    {
        lock (_gate)
        {
            if (_closeRequested) return;
            _closeRequested = true;
        }

        _requestClose();
    }

    private static IGameExitSignal? OpenGameProcessExitSignal()
    {
        var process = Process.GetProcessesByName(GameProcessName).FirstOrDefault();
        return process == null ? null : new ProcessExitSignal(process);
    }

    /// Wraps <see cref="Process.Exited"/>, which is raised from the
    /// process handle becoming signalled — no polling involved.
    private sealed class ProcessExitSignal : IGameExitSignal
    {
        private readonly Process _process;

        public event Action? Exited;

        public ProcessExitSignal(Process process)
        {
            _process = process;
            _process.EnableRaisingEvents = true;
            _process.Exited += (_, _) => Exited?.Invoke();

            // Guard the race where the client exits between enumeration
            // and EnableRaisingEvents: Exited would never fire.
            if (_process.HasExited) Exited?.Invoke();
        }
    }
}
