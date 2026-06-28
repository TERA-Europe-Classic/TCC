using System;
using System.Runtime.InteropServices;

namespace TCC.Utilities;

public static class TrayMessageUtils
{
    public const string TccCloseMessageName = "TCC.ClassicPlus.RequestClose";

    public static int GetTaskbarCreatedMessageId(Func<string, int>? registerMessage = null)
    {
        return (registerMessage ?? RegisterWindowMessage)("TaskbarCreated");
    }

    public static int GetTccCloseMessageId(Func<string, int>? registerMessage = null)
    {
        return (registerMessage ?? RegisterWindowMessage)(TccCloseMessageName);
    }

    public static bool IsTaskbarCreatedMessage(int messageId, int registeredId)
    {
        return registeredId != 0 && messageId == registeredId;
    }

    public static bool IsTccCloseMessage(int messageId, int registeredId, IntPtr targetProcessId, int currentProcessId)
    {
        if (registeredId == 0 || messageId != registeredId) return false;

        var target = targetProcessId.ToInt64();
        return target == 0 || target == currentProcessId;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegisterWindowMessage(string lpString);
}
