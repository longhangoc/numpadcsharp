using System;
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
        switch (_settings.OverlaySize)
        {
            case OverlaySize.Small:
                SmallSizeRadio.IsChecked = true;
                break;
            case OverlaySize.Large:
                LargeSizeRadio.IsChecked = true;
                break;
            default:
                MediumSizeRadio.IsChecked = true;
                break;
        }

        foreach (ComboBoxItem item in HotkeyKeyCombo.Items)
        {
            if (item.Tag is string tag && TryParseHex(tag, out var value) && value == _settings.ToggleHotkeyVirtualKey)
            {
                HotkeyKeyCombo.SelectedItem = item;
                break;
            }
        }
    }

    private void SizeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && radioButton.Tag is string tag
            && Enum.TryParse<OverlaySize>(tag, out var size))
        {
            _settings.OverlaySize = size;
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
}
