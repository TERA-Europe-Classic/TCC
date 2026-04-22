using System;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Forms;
using Nostrum.WPF;
using TCC.Utilities;

namespace TCC.UI.Controls;

public class TccTrayIcon
{
    private bool _connected;
    private NotifyIcon _trayIcon;
    private readonly ContextMenu _contextMenu;
    private readonly Icon? _defaultIcon;
    private readonly Icon? _connectedIcon;
    private readonly TrayMessageWindow _messageWindow;

    public bool Connected
    {
        get => _connected;
        set
        {
            if (_connected == value) return;
            _connected = value;

            _trayIcon.Icon = _connected ? _connectedIcon : _defaultIcon;
        }
    }

    public string Text
    {
        get => _trayIcon.Text;
        set => _trayIcon.Text = value;
    }

    public TccTrayIcon()
    {
        _defaultIcon = MiscUtils.GetEmbeddedIcon("resources/tcc_off.ico");
        _connectedIcon = MiscUtils.GetEmbeddedIcon("resources/tcc_on.ico");

        _contextMenu = new ContextMenu();
        _contextMenu.Items.Add(new MenuItem { Header = "Dashboard", Command = new RelayCommand(_ => WindowManager.DashboardWindow.ShowWindow()) });
        _contextMenu.Items.Add(new MenuItem { Header = "Settings", Command = new RelayCommand(_ => WindowManager.SettingsWindow.ShowWindow()) });
        _contextMenu.Items.Add(new MenuItem
        {
            Header = "Close",
            Command = new RelayCommand(_ =>
            {
                _contextMenu.Closed += (_, _) => App.Close();
                _contextMenu.IsOpen = false;
            })
        });

        _trayIcon = CreateNotifyIcon();
        _messageWindow = new TrayMessageWindow(() => App.BaseDispatcher.InvokeAsync(RecreateTrayIcon));
        RecreateTrayIcon();
    }

    private NotifyIcon CreateNotifyIcon()
    {
        var trayIcon = new NotifyIcon
        {
            Icon = _connected ? _connectedIcon : _defaultIcon,
            Visible = true,
            Text = $"{App.AppVersion} - {(_connected ? "connected" : "not connected")}",
        };
        trayIcon.MouseDown += OnMouseDown;
        trayIcon.MouseDoubleClick += (_, _) => WindowManager.SettingsWindow.ShowWindow();
        return trayIcon;
    }

    private void RecreateTrayIcon()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _trayIcon = CreateNotifyIcon();
        _trayIcon.Visible = true;
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        _contextMenu.IsOpen = e.Button switch
        {
            MouseButtons.Right => true,
            MouseButtons.Left => false,
            _ => _contextMenu.IsOpen
        };
    }

    public void Dispose()
    {
        _messageWindow.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }

    private sealed class TrayMessageWindow : NativeWindow, IDisposable
    {
        private readonly Action _onTaskbarCreated;
        private readonly int _taskbarCreatedMessageId;

        public TrayMessageWindow(Action onTaskbarCreated)
        {
            _onTaskbarCreated = onTaskbarCreated;
            _taskbarCreatedMessageId = TrayMessageUtils.GetTaskbarCreatedMessageId();
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (TrayMessageUtils.IsTaskbarCreatedMessage(m.Msg, _taskbarCreatedMessageId))
            {
                _onTaskbarCreated();
            }

            base.WndProc(ref m);
        }

        public void Dispose()
        {
            DestroyHandle();
        }
    }
}
