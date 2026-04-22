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

    [Theory]
    [InlineData(1337, 1337, true)]
    [InlineData(1338, 1337, false)]
    [InlineData(0, 1337, false)]
    [InlineData(1337, 0, false)]
    public void DetectsTaskbarCreatedMessage(int messageId, int registeredId, bool expected)
    {
        Assert.Equal(expected, TrayMessageUtils.IsTaskbarCreatedMessage(messageId, registeredId));
    }
}
