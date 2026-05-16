using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using NumpadOverlay.Models;

namespace NumpadOverlay;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        DataContext = _settings;
        Loaded += SettingsWindow_Loaded;
    }

    private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        foreach (ComboBoxItem item in HotkeyKeyCombo.Items)
        {
            if (item.Tag is string tag && TryParseHex(tag, out var value) && value == _settings.ToggleHotkeyVirtualKey)
            {
                HotkeyKeyCombo.SelectedItem = item;
                break;
            }
        }
    }



    private void HotkeyKeyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HotkeyKeyCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag
            && TryParseHex(tag, out var value))
        {
            _settings.ToggleHotkeyVirtualKey = value;
        }
    }

    private static bool TryParseHex(string input, out int value)
    {
        value = 0;
        if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            input = input[2..];
        }

        return int.TryParse(input, System.Globalization.NumberStyles.HexNumber, null, out value);
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.Save();
        System.Windows.Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _settings.Save();
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateButton.Content = "Đang kiểm tra...";
        UpdateButton.IsEnabled = false;
        await CheckForUpdateAsync();
        UpdateButton.Content = "Kiểm tra cập nhật";
        UpdateButton.IsEnabled = true;
    }

    private async Task CheckForUpdateAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("OpenNP");
            var json = await client.GetStringAsync("https://api.github.com/repos/longhangoc/numpadcsharp/releases/latest");
            using var doc = JsonDocument.Parse(json);
            var tag = doc.RootElement.GetProperty("tag_name").GetString();
            if (Version.TryParse(tag?.TrimStart('v'), out var latest) && latest > Assembly.GetEntryAssembly()!.GetName().Version)
            {
                var body = doc.RootElement.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
                var updateWin = new UpdateWindow(Assembly.GetEntryAssembly()!.GetName().Version!.ToString(), latest.ToString(), body, doc.RootElement.GetProperty("assets"));
                updateWin.ShowDialog();
            }
            else
            {
                System.Windows.MessageBox.Show("Đang dùng bản mới nhất.");
            }
        }
        catch
        {
            System.Windows.MessageBox.Show("Lỗi kiểm tra cập nhật.");
        }
    }

}
