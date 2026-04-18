// Classic+ read-only fork: no-op compatibility shim.
//
// The upstream StubInterface/StubClient pair brokered every outbound write TCC
// ever made — LFG RPCs, group-window buttons, player-menu actions, chat-link
// replay, broker accept/decline, slash-command dispatch, and so on. DESIGN.md
// §"TCC write paths to delete" enumerates all of it.
//
// This shim preserves the full call-site API surface so existing TCC.Core
// code compiles without editing dozens of files, while guaranteeing zero
// outbound RPC at runtime: every method is a no-op, every availability flag
// is false. UI bindings like `ShowLeaveButton => Formed &&
// StubInterface.Instance.IsStubAvailable` therefore auto-hide write-only
// buttons without any view-model edits.

using System.Threading.Tasks;

namespace TCC.Interop.Proxy;

public sealed class StubInterface
{
    public static StubInterface Instance { get; } = new();

    private StubInterface() { StubClient = new StubClient(); }

    public bool IsStubAvailable => false;
    public bool IsFpsModAvailable => false;
    public bool IsConnected => false;

    public StubClient StubClient { get; }

    public Task InitAsync(bool lfgEnabled, bool chatEnabled, bool playerMenuEnabled)
    {
        _ = lfgEnabled; _ = chatEnabled; _ = playerMenuEnabled;
        return Task.CompletedTask;
    }

    public void Disconnect() { /* no-op */ }
}
