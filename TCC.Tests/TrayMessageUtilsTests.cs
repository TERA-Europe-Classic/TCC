using TCC.Utilities;

namespace TCC.Tests;

public class TrayMessageUtilsTests
{
    [Fact]
    public void RegistersTaskbarCreatedMessageByName()
    {
        string? registeredName = null;

        var messageId = TrayMessageUtils.GetTaskbarCreatedMessageId(name =>
        {
            registeredName = name;
            return 1337;
        });

        Assert.Equal("TaskbarCreated", registeredName);
        Assert.Equal(1337, messageId);
    }

    [Fact]
    public void RegistersTccCloseMessageByName()
    {
        string? registeredName = null;

        var messageId = TrayMessageUtils.GetTccCloseMessageId(name =>
        {
            registeredName = name;
            return 7331;
        });

        Assert.Equal("TCC.ClassicPlus.RequestClose", registeredName);
        Assert.Equal(7331, messageId);
    }

    [Theory]
    [InlineData(1337, 1337, true)]
    [InlineData(1338, 1337, false)]
    [InlineData(0, 1337, false)]
    [InlineData(1337, 0, false)]
    public void DetectsTaskbarCreatedMessage(int messageId, int registeredId, bool expected)
    {
        Assert.Equal(expected, TrayMessageUtils.IsTaskbarCreatedMessage(messageId, registeredId));
    }

    [Theory]
    [InlineData(7331, 7331, 42, 42, true)]
    [InlineData(7331, 7331, 0, 42, true)]
    [InlineData(7332, 7331, 42, 42, false)]
    [InlineData(7331, 0, 42, 42, false)]
    [InlineData(7331, 7331, 41, 42, false)]
    public void DetectsTccCloseMessageForCurrentProcess(
        int messageId,
        int registeredId,
        int targetProcessId,
        int currentProcessId,
        bool expected)
    {
        Assert.Equal(
            expected,
            TrayMessageUtils.IsTccCloseMessage(
                messageId,
                registeredId,
                new IntPtr(targetProcessId),
                currentProcessId));
    }
}
