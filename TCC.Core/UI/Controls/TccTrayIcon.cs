using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Nostrum.WPF;
using TCC.Utilities;

namespace TCC.UI.Controls;

public class TccTrayIcon
{
    private bool _connected;
    private readonly TrayIconLifecycle<NotifyIcon> _trayIconLifecycle;
    private readonly ContextMenu _contextMenu;
    private readonly Icon _defaultIcon;
    private readonly Icon _connectedIcon;
    private readonly TrayMessageWindow _messageWindow;

    public bool Connected
    {
        get => _connected;
        set
        {
            if (_connected == value) return;
            _connected = value;

            _trayIconLifecycle.Current.Icon = _connected ? _connectedIcon : _defaultIcon;
        }
    }

    public string Text
    {
        get => _trayIconLifecycle.Current.Text;
        set => _trayIconLifecycle.Current.Text = value;
    }

    public TccTrayIcon()
    {
        _defaultIcon = TrayIconResources.Load("resources/tcc_off.ico");
        _connectedIcon = TrayIconResources.Load("resources/tcc_on.ico");

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

        _trayIconLifecycle = new TrayIconLifecycle<NotifyIcon>(CreateNotifyIcon, (icon, visible) => icon.Visible = visible);
        _messageWindow = new TrayMessageWindow(() => App.BaseDispatcher.InvokeAsync(RecreateTrayIcon));
    }

    private NotifyIcon CreateNotifyIcon()
    {
        var trayIcon = new NotifyIcon
        {
            Icon = _connected ? _connectedIcon : _defaultIcon,
            Text = $"{App.AppVersion} - {(_connected ? "connected" : "not connected")}",
        };
        trayIcon.MouseDown += OnMouseDown;
        trayIcon.MouseDoubleClick += (_, _) => WindowManager.SettingsWindow.ShowWindow();
        return trayIcon;
    }

    private void RecreateTrayIcon()
    {
        _trayIconLifecycle.Recreate();
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
        _trayIconLifecycle.Dispose();
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

public static class TrayIconResources
{
    public static Icon Load(string resourcePath)
    {
        return LoadFromTccResource(resourcePath)
            ?? LoadFromFile(resourcePath)
            ?? LoadFromExecutable()
            ?? (Icon)SystemIcons.Application.Clone();
    }

    private static Icon? LoadFromTccResource(string resourcePath)
    {
        try
        {
            var assemblyName = typeof(TrayIconResources).Assembly.GetName().Name;
            var normalizedPath = resourcePath.Replace('\\', '/');
            var uri = new Uri($"pack://application:,,,/{assemblyName};component/{normalizedPath}", UriKind.Absolute);
            var resource = System.Windows.Application.GetResourceStream(uri);
            if (resource?.Stream == null) return null;

            using var stream = resource.Stream;
            return new Icon(stream);
        }
        catch
        {
            return null;
        }
    }

    private static Icon? LoadFromFile(string resourcePath)
    {
        try
        {
            var relativePath = resourcePath.Replace('/', Path.DirectorySeparatorChar);
            var path = Path.Combine(App.BasePath, relativePath);
            return File.Exists(path) ? new Icon(path) : null;
        }
        catch
        {
            return null;
        }
    }

    private static Icon? LoadFromExecutable()
    {
        try
        {
            var processPath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            return string.IsNullOrWhiteSpace(processPath) ? null : Icon.ExtractAssociatedIcon(processPath);
        }
        catch
        {
            return null;
        }
    }
}

public sealed class TrayIconLifecycle<T> : IDisposable where T : IDisposable
{
    private readonly Func<T> _createIcon;
    private readonly Action<T, bool> _setVisible;

    public T Current { get; private set; }

    public TrayIconLifecycle(Func<T> createIcon, Action<T, bool> setVisible)
    {
        _createIcon = createIcon;
        _setVisible = setVisible;
        Current = CreateVisibleIcon();
    }

    public void Recreate()
    {
        _setVisible(Current, false);
        Current.Dispose();
        Current = CreateVisibleIcon();
    }

    public void Dispose()
    {
        _setVisible(Current, false);
        Current.Dispose();
    }

    private T CreateVisibleIcon()
    {
        var icon = _createIcon();
        _setVisible(icon, true);
        return icon;
    }
}
