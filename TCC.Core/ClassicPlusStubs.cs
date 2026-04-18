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

        // Tester.cs builds fake listings; Players + Applicants must exist as
        // observable collections it can .Add() to.
        public ObservableCollection<User> Players { get; } = new();
        public ObservableCollection<User> Applicants { get; } = new();
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

        public ObservableCollection<Data.Listing> Listings { get; } = new();

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
        public LfgListWindow(ViewModels.LfgListViewModel vm) { _ = vm; }
        public LfgListWindow() { }
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
