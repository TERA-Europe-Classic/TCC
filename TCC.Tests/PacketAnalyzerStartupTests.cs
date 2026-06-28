using System.Text.RegularExpressions;

namespace TCC.Tests;

public class PacketAnalyzerStartupTests
{
    [Fact]
    public void ProcessorHooksAreInstalledBeforeSnifferCanEmitPackets()
    {
        var analyzer = File.ReadAllText(Path.Combine(FindRepoRoot().FullName, "TeraPacketParser", "Analysis", "PacketAnalyzer.cs"));
        var init = ExtractMethodBody(analyzer, "private static void Init");

        AssertInOrder(init,
            "Processor = new MessageProcessor();",
            "ProcessorReady?.Invoke()",
            "Sniffer.MessageReceived += EnqueuePacket;",
            "Sniffer.Enabled = true;",
            "AnalysisThread.Start();");
    }

    [Fact]
    public void PacketLoopDoesNotPublishProcessorReadyAfterConsumingMayStart()
    {
        var analyzer = File.ReadAllText(Path.Combine(FindRepoRoot().FullName, "TeraPacketParser", "Analysis", "PacketAnalyzer.cs"));
        var loop = ExtractMethodBody(analyzer, "private static void PacketAnalysisLoop");

        Assert.DoesNotContain("ProcessorReady", loop);
        Assert.DoesNotContain("new MessageProcessor", loop);
    }

    private static void AssertInOrder(string source, params string[] needles)
    {
        var previous = -1;
        foreach (var needle in needles)
        {
            var current = source.IndexOf(needle, StringComparison.Ordinal);
            Assert.True(current >= 0, $"Expected to find '{needle}' in source.");
            Assert.True(current > previous, $"Expected '{needle}' to appear after the previous checkpoint.");
            previous = current;
        }
    }

    private static string ExtractMethodBody(string source, string signature)
    {
        var match = Regex.Match(source, Regex.Escape(signature) + @"\([^)]*\)\s*\{", RegexOptions.Multiline);
        Assert.True(match.Success, $"Could not find method '{signature}'.");

        var start = match.Index;
        var depth = 0;
        for (var i = match.Index; i < source.Length; i++)
        {
            switch (source[i])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                    {
                        return source[start..(i + 1)];
                    }
                    break;
            }
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
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
