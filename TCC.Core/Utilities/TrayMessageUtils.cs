using System;
using System.Runtime.InteropServices;

namespace TCC.Utilities;

public static class TrayMessageUtils
{
    public static int GetTaskbarCreatedMessageId(Func<string, int>? registerMessage = null)
    {
        return (registerMessage ?? RegisterWindowMessage)("TaskbarCreated");
    }

    public static bool IsTaskbarCreatedMessage(int messageId, int registeredId)
    {
        return registeredId != 0 && messageId == registeredId;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegisterWindowMessage(string lpString);
}
