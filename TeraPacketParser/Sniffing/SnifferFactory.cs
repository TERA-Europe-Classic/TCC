using TeraPacketParser.TeraCommon.Sniffing;

namespace TeraPacketParser.Sniffing;

public static class SnifferFactory
{
    /// <summary>
    /// Classic+ is a strictly read-only, mirror-socket build. The pcap and
    /// Toolbox-RPC sniffers are gone; all captures flow through the
    /// ClassicPlusSniffer (127.0.0.1:7803, see ClassicPlusSniffer.cs for the
    /// protocol). Both overloads exist to stay source-compatible with any
    /// legacy call sites that still pass CaptureMode / toolboxMode.
    /// </summary>
    public static ITeraSniffer Create() => new ClassicPlusSniffer();

    public static ITeraSniffer Create(CaptureMode mode, bool toolboxMode) => new ClassicPlusSniffer();
}
