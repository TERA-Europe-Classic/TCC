// Classic+ read-only fork: type stubs.
//
// The strip pass deleted every file that implemented LFG, Moongourd, or
// other purely-write-path features (see DESIGN.md § TCC write paths to
// delete). That left cross-cutting references to the *type names* in files
// we want to keep: SettingsContainer persists them, SystemMessagesProcessor
// / Game / Tester / ChatManager call methods on them, WindowManager has
// fields of those types.
//
// Instead of touching every referrer, we reinstate the type names here as
// minimal, no-op shims. Collection-shaped members return empty collections
// so ForEach / Count / ToSyncList loops safely do nothing; methods return
// void; properties settle at default values. At runtime, the sniffer never
// feeds LFG packets (ClassicPlusSniffer filter) so these objects are never
// populated — they exist only to satisfy the compiler and the persistence
// layer that round-trips settings.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;
using Nostrum.WPF.ThreadSafe;
using TCC.Data.Pc;
using TCC.Settings.WindowSettings;
using TCC.UI.Windows;

namespace TCC.Interop
{
    // Firebase / Cloud stubs — their classes were deleted with the telemetry
    // strip. Keep the names resolvable so existing call sites compile; every
    // method no-ops.
    public static class Firebase
    {
        public static System.Threading.Tasks.Task<bool> RequestWebhookExecution(string webhook, string accountHash)
        { _ = webhook; _ = accountHash; return System.Threading.Tasks.Task.FromResult(false); }
        // Game.cs passes (url, bool, accountHash); the original library had a
        // multi-arg overload. Stubbed to no-op.
        public static System.Threading.Tasks.Task<bool> RegisterWebhook(string webhookUrl, bool something, string accountHash)
        { _ = webhookUrl; _ = something; _ = accountHash; return System.Threading.Tasks.Task.FromResult(false); }
        public static void Ping() { }
        // Called from App.xaml.cs shutdown + GlobalExceptionHandler + Game.OnDisconnected.
        public static void Dispose() { }
    }

    public static class Cloud
    {
        public static void SendUsageStats(string region, string server, string accountHash, string version)
        { _ = region; _ = server; _ = accountHash; _ = version; }
        public static void Init() { }
        public static bool UsageStatsEnabled { get; set; }
        // Called by Game.SendUsageStat + Tester.SendFakeUsageStat. Returns
        // false so the daily-stat-sent update guard in Game.cs short-circuits.
        public static System.Threading.Tasks.Task<bool> SendUsageStatAsync(
            string region, uint serverId, string ip, string serverName,
            string account, string version, bool isDailyFirst)
        {
            _ = region; _ = serverId; _ = ip; _ = serverName; _ = account; _ = version; _ = isDailyFirst;
            return System.Threading.Tasks.Task.FromResult(false);
        }
    }
}

namespace TCC.Interop.Proxy
{
    // StubMessageParser existed to decode toolbox-pushed packets. With
    // ClassicPlusSniffer pulling raw encrypted frames from 127.0.0.1:7803
    // there's nothing to register — every registration call is a no-op.
    // Events are exposed so Game.cs += handler calls compile; they never fire.
    public static class StubMessageParser
    {
        public static void Register<T>() where T : class { }
        public static void RegisterHandler<T>(Action<T> handler) where T : class { _ = handler; }

        public static event Action<bool>? SetUiModeEvent;
        public static event Action<bool>? SetChatModeEvent;
        public static event Action<string, uint, string>? HandleChatMessageEvent;
        public static event Action<string, uint, string, bool>? HandleTranslatedMessageEvent;
        public static event Action<TeraPacketParser.Message>? HandleRawPacketEvent;

        // Reference event backing fields so the compiler doesn't warn about
        // unused events (they are never raised in read-only mode).
        static StubMessageParser()
        {
            _ = SetUiModeEvent;
            _ = SetChatModeEvent;
            _ = HandleChatMessageEvent;
            _ = HandleTranslatedMessageEvent;
            _ = HandleRawPacketEvent;
        }
    }
}

namespace TCC.UI.Controls.Chat
{
    // FriendMessageDialog was a popup asking "add as friend?" before writing
    // an RPC. Now stubbed — PlayerMenuViewModel calls its ShowDialog path;
    // returns null so the caller gracefully skips the write.
    public class FriendMessageDialog : System.Windows.Window
    {
        // PlayerMenuViewModel calls both ctor shapes — parameterless when
        // adding a friend fresh, with-name when prefilling. Support both.
        public FriendMessageDialog() { }
        public FriendMessageDialog(string name) { _ = name; }
        public string Message { get; set; } = "";
        public new bool? ShowDialog() => false;
    }
}

namespace TCC.UI.Windows
{
    // LfgFilterConfigWindow let the user pick LFG level filters before a
    // RequestListings RPC. Stubbed window so the Settings "Configure filters"
    // button still compiles; ShowDialog is a no-op.
    public class LfgFilterConfigWindow : System.Windows.Window
    {
        public LfgFilterConfigWindow(object lfgVm) { _ = lfgVm; }
        public new bool? ShowDialog() => false;
    }
}

namespace TCC.Settings.WindowSettings
{
    public class LfgWindowSettings : WindowSettingsBase
    {
        public bool HideTradeListings { get; set; }
        public int MinLevel { get; set; } = 1;
        public int MaxLevel { get; set; } = 70;
    }
}

namespace TCC.Data
{
    /// <summary>
    /// Stub for the deleted Listing type. Populated only by LFG packet handlers
    /// that no longer fire in read-only mode, so the values are never observed.
    /// </summary>
    public class Listing : ThreadSafeObservableObject
    {
        public uint LeaderId { get; set; }
        public uint ServerId { get; set; }
        public string LeaderName { get; set; } = "";
        public string Message { get; set; } = "";
        public bool IsRaid { get; set; }
        public bool IsTrade { get; set; }
        public bool IsMyLfg { get; set; }
        public bool IsExpanded { get; set; }
        public bool IsPopupOpen { get; set; }
        public bool CanApply { get; set; }
        // `Temp` is checked by ListingTemplateSelector + DataTemplates.xaml.cs
        // to decide whether the listing is a provisional placeholder. In
        // read-only mode we never create placeholders; always false.
        public bool Temp { get; set; }

        // Tester.cs builds fake listings; Players + Applicants must exist as
        // thread-safe observable collections (so `.ToSyncList()` extension
        // resolves) that also expose `.Add()` for the debug helpers.
        public Nostrum.WPF.ThreadSafe.ThreadSafeObservableCollection<User> Players { get; } = new();
        public Nostrum.WPF.ThreadSafe.ThreadSafeObservableCollection<User> Applicants { get; } = new();
    }
}

namespace TCC.ViewModels
{
    /// <summary>
    /// Stub for the deleted LfgListViewModel. Exposes an empty listings
    /// collection and no-op methods for every caller in SystemMessagesProcessor,
    /// Game, Tester, LfgMessage.
    /// </summary>
    public class LfgListViewModel
    {
        public LfgListViewModel(LfgWindowSettings settings) { _ = settings; }

        // ThreadSafeObservableCollection so `.ToSyncList()` resolves without
        // editing LfgMessage.cs FindListing.
        public Nostrum.WPF.ThreadSafe.ThreadSafeObservableCollection<Data.Listing> Listings { get; } = new();

        public void EnqueueRequest(uint leaderId, uint serverId) { _ = leaderId; _ = serverId; }
        public void EnqueueListRequest() { }
        public void RemoveDeadLfg() { }
        public void ForceStopPublicize() { }
        public void AddOrRefreshListing(Data.Listing listing) { _ = listing; }
    }
}

namespace TCC.UI.Windows
{
    /// <summary>
    /// Stub for the deleted LfgListWindow. Kept so WindowManager.LfgListWindow
    /// property and Tester's `WindowManager.LfgListWindow.Dispatcher` calls
    /// still compile. ShowWindow is a no-op so nothing ever appears on screen.
    /// </summary>
    public class LfgListWindow : TccWindow
    {
        // TccWindow's only ctor is `protected TccWindow(bool canClose)`.
        // Pass false so the chromed window can't be closed independently if
        // something in the codebase does manage to instantiate this stub
        // (shouldn't, WindowManager.LfgListWindow is set but ShowWindow is
        // never reached in normal flow).
        public LfgListWindow(ViewModels.LfgListViewModel vm) : base(false) { _ = vm; }
        public LfgListWindow() : base(false) { }
        public new void ShowWindow() { /* no-op */ }
    }
}

namespace TCC.UI.Controls.Chat
{
    /// <summary>
    /// Stub for the deleted MoongourdPopupViewModel. PlayerMenuViewModel
    /// instantiates it and invokes RequestInfo on right-click; in read-only
    /// mode we don't hit moongourd.com, so this is a silent no-op.
    /// </summary>
    public class MoongourdPopupViewModel : ThreadSafeObservableObject
    {
        public string PlayerName { get; set; } = "";
        public void RequestInfo(string name, object region) { _ = name; _ = region; }
    }
}
