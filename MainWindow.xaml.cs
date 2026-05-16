using System;
using System.Collections.Generic;
using System.ComponentModel;
using SD = System.Drawing;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using NumpadOverlay.Models;

namespace NumpadOverlay;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const int WhKeyboardLl = 13;
    private const int WmKeydown = 0x0100;
    private const int WmKeyup = 0x0101;
    private const int WmSysKeydown = 0x0104;
    private const int WmSysKeyup = 0x0105;
    private const int WmHotkey = 0x0312;
    private const int ModControl = 0x0002;
    private const int VkOem3 = 0xC0;
    private const int HotkeyId = 9000;

    private static readonly System.Windows.Media.Color BaseKeyColor = System.Windows.Media.Color.FromRgb(22, 33, 62);
    private static readonly System.Windows.Media.Color PressedKeyColor = System.Windows.Media.Color.FromRgb(233, 69, 96);

    private readonly AppSettings _settings;
    private readonly Dictionary<int, Border> _keyBorders;
    private readonly Dictionary<Border, SolidColorBrush> _borderBrushes = new();
    private readonly HashSet<int> _activeKeys = new();
    private readonly HashSet<int> _modifierKeys = new();

    private WinForms.NotifyIcon? _trayIcon;
    private int _currentHotkeyVKey;
    private WinForms.ToolStripMenuItem? _showHideMenuItem;
    private SettingsWindow? _settingsWindow;
    private Thread? _hookThread;
    private IntPtr _keyboardHook = IntPtr.Zero;
    private HwndSource? _hookSource;
    private LowLevelKeyboardProc? _keyboardProc;

    public MainWindow()
    {
        InitializeComponent();

        _settings = AppSettings.Load();
        DataContext = _settings;
        _settings.PropertyChanged += Settings_PropertyChanged;
        _currentHotkeyVKey = _settings.ToggleHotkeyVirtualKey;

        _keyBorders = new Dictionary<int, Border>
        {
            [0x90] = NumLockKey,
            [0x6F] = DivideKey,
            [0x6A] = MultiplyKey,
            [0x6D] = SubtractKey,
            [0x6B] = AddKey,
            [0x6E] = DecimalKey,
            [0x0D] = EnterKey,
            [0x08] = BackspaceKey,
            [0x60] = ZeroKey,
            [0x61] = Key1,
            [0x62] = Key2,
            [0x63] = Key3,
            [0x64] = Key4,
            [0x65] = Key5,
            [0x66] = Key6,
            [0x67] = Key7,
            [0x68] = Key8,
            [0x69] = Key9,
        };

        InitializeBorderBrushes();
        UpdateNumLockStatus();
        ApplySettings();
        InitializeTrayIcon();
        StartKeyboardHookThread();
        _ = CheckForUpdateAsync();

        VersionText.Text = $"v{Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0"}";
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (e.PropertyName == nameof(AppSettings.Opacity))
            {
                Opacity = _settings.Opacity;
            }
            else if (e.PropertyName == nameof(AppSettings.OverlayScale))
            {
                ApplySettings();
            }
            else if (e.PropertyName == nameof(AppSettings.ToggleHotkeyVirtualKey))
            {
                if (_hookSource?.Dispatcher != null)
                {
                    _hookSource.Dispatcher.BeginInvoke(RegisterToggleHotkey);
                }
            }
        });
    }

    private void ApplySettings()
    {
        Opacity = _settings.Opacity;
        LayoutTransform = new ScaleTransform(_settings.Scale, _settings.Scale);
    }

    private void InitializeTrayIcon()
    {
        var menu = new WinForms.ContextMenuStrip();
        _showHideMenuItem = new WinForms.ToolStripMenuItem("Ẩn overlay", null, ShowHideMenuItem_Click);
        menu.Items.Add(_showHideMenuItem);
        menu.Items.Add(new WinForms.ToolStripMenuItem("Cài đặt", null, OpenSettingsMenuItem_Click));
        menu.Items.Add(new WinForms.ToolStripMenuItem("Thoát", null, ExitMenuItem_Click));

        _trayIcon = new WinForms.NotifyIcon
        {
            Icon = SD.SystemIcons.Application,
            Text = "OpenNP",
            Visible = true,
            ContextMenuStrip = menu
        };

        _trayIcon.DoubleClick += (sender, args) => ToggleOverlay();
    }

    private void StartKeyboardHookThread()
    {
        _hookThread = new Thread(KeyboardHookThreadProc)
        {
            IsBackground = true
        };
        _hookThread.SetApartmentState(ApartmentState.STA);
        _hookThread.Start();
    }

    private void KeyboardHookThreadProc()
    {
        _keyboardProc = LowLevelKeyboardProcImpl;

        var parameters = new HwndSourceParameters("OpenNPHookWindow")
        {
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
            WindowStyle = 0x800000
        };

        _hookSource = new HwndSource(parameters);
        _hookSource.AddHook(HwndMessageHook);
        _keyboardHook = SetWindowsHookEx(WhKeyboardLl, _keyboardProc, GetModuleHandle(null), 0);
        RegisterToggleHotkey();

        Dispatcher.Run();

        UnregisterHotKey(_hookSource.Handle, HotkeyId);
        _hookSource.RemoveHook(HwndMessageHook);
        _hookSource.Dispose();
        _hookSource = null;
    }

    private IntPtr HwndMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            Dispatcher.Invoke(ToggleOverlay);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void RegisterToggleHotkey()
    {
        if (_hookSource is null)
            return;

        if (_currentHotkeyVKey != 0)
        {
            UnregisterHotKey(_hookSource.Handle, HotkeyId);
        }

        _currentHotkeyVKey = _settings.ToggleHotkeyVirtualKey;
        RegisterHotKey(_hookSource.Handle, HotkeyId, ModControl, _currentHotkeyVKey);
    }

    private IntPtr LowLevelKeyboardProcImpl(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var message = wParam.ToInt32();
            if (message == WmKeydown || message == WmSysKeydown || message == WmKeyup || message == WmSysKeyup)
            {
                var vkCode = Marshal.ReadInt32(lParam);
                var isDown = message == WmKeydown || message == WmSysKeydown;
                Dispatcher.BeginInvoke(() => ProcessKeyState(vkCode, isDown));
            }
        }

        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private void ProcessKeyState(int vkCode, bool isDown)
    {
        if (vkCode == 0x90)
        {
            UpdateNumLockStatus();
        }

        if (IsModifierKey(vkCode))
        {
            UpdateModifierState(vkCode, isDown);
            return;
        }

        if (!_keyBorders.TryGetValue(vkCode, out var border))
            return;

        if (isDown)
        {
            if (_activeKeys.Add(vkCode))
                SetKeyPressedVisual(border);
        }
        else
        {
            if (_activeKeys.Remove(vkCode))
                ResetKeyVisual(border);
        }
    }

    private static bool IsModifierKey(int vkCode)
    {
        return vkCode == 0x10 || vkCode == 0x11 || vkCode == 0x12;
    }

    private void UpdateModifierState(int vkCode, bool isDown)
    {
        if (isDown)
            _modifierKeys.Add(vkCode);
        else
            _modifierKeys.Remove(vkCode);
    }

    private void UpdateNumLockStatus()
    {
        try
        {
            var isNumLockOn = WinForms.Control.IsKeyLocked(WinForms.Keys.NumLock);
            NumLockStatusText.Text = isNumLockOn ? "NumLock: BẬT" : "NumLock: TẮT";
            NumLockStatusBorder.Background = isNumLockOn ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 160, 75)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 33, 62));
        }
        catch
        {
            NumLockStatusText.Text = "NumLock";
        }
    }

    private void SetKeyPressedVisual(Border border)
    {
        if (!_borderBrushes.TryGetValue(border, out var brush))
            return;

        brush.Color = PressedKeyColor;
        border.Opacity = 1.0;
    }

    private void ResetKeyVisual(Border border)
    {
        if (!_borderBrushes.TryGetValue(border, out var brush))
            return;

        brush.Color = BaseKeyColor;
        border.Opacity = 0.95;
    }

    private void ToggleOverlay()
    {
        if (Visibility == Visibility.Visible)
        {
            Hide();
            _showHideMenuItem!.Text = "Hiện overlay";
        }
        else
        {
            Show();
            Activate();
            _showHideMenuItem!.Text = "Ẩn overlay";
        }
    }

    private void ShowHideMenuItem_Click(object? sender, EventArgs e)
    {
        ToggleOverlay();
    }

    private void OpenSettingsMenuItem_Click(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(OpenSettingsWindow);
    }

    private void ExitMenuItem_Click(object? sender, EventArgs e)
    {
        _settings.Save();
        System.Windows.Application.Current.Shutdown();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsWindow();
    }

    private void OpenSettingsWindow()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings)
        {
            Owner = this
        };

        _settingsWindow.Closed += (sender, args) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_settings.WindowLeft > 0 && _settings.WindowTop > 0)
        {
            Left = _settings.WindowLeft;
            Top = _settings.WindowTop;
        }
    }

    private void Window_LocationChanged(object sender, EventArgs e)
    {
        if (IsLoaded && WindowState == WindowState.Normal)
        {
            _settings.WindowLeft = Left;
            _settings.WindowTop = Top;
        }
    }

    private void ResizeGrip_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {
        Width = Math.Max(MinWidth, Width + e.HorizontalChange);
        Height = Math.Max(MinHeight, Height + e.VerticalChange);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (WindowState == WindowState.Normal)
        {
            _settings.WindowLeft = Left;
            _settings.WindowTop = Top;
        }
        _settings.Save();
        DisposeTrayIcon();
        CleanupKeyboardHook();
    }

    private void DisposeTrayIcon()
    {
        if (_trayIcon is null)
            return;

        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _trayIcon = null;
    }

    private void CleanupKeyboardHook()
    {
        if (_hookSource?.Dispatcher != null)
        {
            _hookSource.Dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
        }

        if (_keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private void InitializeBorderBrushes()
    {
        foreach (var border in _keyBorders.Values)
        {
            if (border is null)
                continue;

            var brush = new SolidColorBrush(BaseKeyColor);
            border.Background = brush;
            _borderBrushes[border] = brush;
        }
    }

    private async Task CheckForUpdateAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("OpenNP");
            var response = await client.GetAsync("https://api.github.com/repos/longhangoc/numpadcsharp/releases/latest");
            if (!response.IsSuccessStatusCode)
                return; // Chưa có release hoặc rate limit

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var tag = doc.RootElement.GetProperty("tag_name").GetString();
            if (Version.TryParse(tag?.TrimStart('v'), out var latest) && latest > Assembly.GetEntryAssembly()!.GetName().Version)
            {
                var body = doc.RootElement.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
                var updateWin = new UpdateWindow(Assembly.GetEntryAssembly()!.GetName().Version!.ToString(), latest.ToString(), body, doc.RootElement.GetProperty("assets"));
                updateWin.ShowDialog();
            }
        }
        catch
        {
            // Silent - không làm phiền người dùng khi start app
        }
    }

}
