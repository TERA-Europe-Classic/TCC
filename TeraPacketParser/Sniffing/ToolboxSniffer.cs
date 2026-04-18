// Classic+ read-only fork: type stub.
//
// Upstream ToolboxSniffer bridged tera-toolbox's data + control channels
// (127.0.0.60:5300/5301 + HTTP JSON-RPC at 127.0.0.61:5300) into TCC's
// ITeraSniffer pipeline. That entire mechanism is replaced by
// ClassicPlusSniffer (127.0.0.1:7803 mirror socket). The type is kept as an
// empty class so legacy `is ToolboxSniffer tbs` pattern-matches against the
// runtime ClassicPlusSniffer return false and the dead control-connection
// branches elide themselves. SnifferFactory never instantiates it.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using TeraPacketParser.TeraCommon.Game;
using TeraPacketParser.TeraCommon.Sniffing;

namespace TeraPacketParser.Sniffing;

public sealed class ToolboxSniffer : ITeraSniffer
{
    public bool Enabled { get; set; }
    public bool Connected { get; set; }

    public event Action<Message>? MessageReceived { add { } remove { } }
    public event Action<Server>? NewConnection { add { } remove { } }
    public event Action? EndConnection { add { } remove { } }

    public ConcurrentQueue<Message> Packets { get; } = new();

    // Retained only so legacy Game.cs line `tbs.ControlConnection.DumpMap(...)`
    // still resolves. Always returns a disconnected, no-op control interface.
    public ToolboxControlInterfaceStub ControlConnection { get; } = new();

    public sealed class ToolboxControlInterfaceStub
    {
        public Task<bool> DumpMap(string path, string name)
        {
            _ = path; _ = name;
            return Task.FromResult(false);
        }
    }
}
