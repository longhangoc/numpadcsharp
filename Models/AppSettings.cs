using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NumpadOverlay.Models;



public class AppSettings : INotifyPropertyChanged
{
    private const string FileName = "settings.json";

    private double _opacity = 0.85;
    private double _overlayScale = 1.0;
    private int _toggleHotkeyVirtualKey = 0xC0; // VK_OEM_3
    private double _windowLeft = -1;
    private double _windowTop = -1;

    [JsonIgnore]
    public static string SettingsFilePath => Path.Combine(AppContext.BaseDirectory, FileName);

    public double Opacity
    {
        get => _opacity;
        set
        {
            if (value.Equals(_opacity))
                return;
            _opacity = Math.Clamp(value, 0.1, 1.0);
            OnPropertyChanged(nameof(Opacity));
        }
    }

    public double OverlayScale
    {
        get => _overlayScale;
        set
        {
            if (value == _overlayScale)
                return;
            _overlayScale = Math.Clamp(value, 0.5, 2.0);
            OnPropertyChanged(nameof(OverlayScale));
            OnPropertyChanged(nameof(Scale));
        }
    }

    public int ToggleHotkeyVirtualKey
    {
        get => _toggleHotkeyVirtualKey;
        set
        {
            if (value == _toggleHotkeyVirtualKey)
                return;
            _toggleHotkeyVirtualKey = value;
            OnPropertyChanged(nameof(ToggleHotkeyVirtualKey));
            OnPropertyChanged(nameof(ToggleHotkeyLabel));
        }
    }

    public double WindowLeft
    {
        get => _windowLeft;
        set
        {
            if (value == _windowLeft) return;
            _windowLeft = value;
            OnPropertyChanged(nameof(WindowLeft));
        }
    }

    public double WindowTop
    {
        get => _windowTop;
        set
        {
            if (value == _windowTop) return;
            _windowTop = value;
            OnPropertyChanged(nameof(WindowTop));
        }
    }

    [JsonIgnore]
    public string ToggleHotkeyLabel => $"Ctrl + {GetVirtualKeyLabel(ToggleHotkeyVirtualKey)}";

    [JsonIgnore]
    public double Scale => OverlayScale;

    public event PropertyChangedEventHandler? PropertyChanged;

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            var settings = JsonSerializer.Deserialize<AppSettings>(json, options);
            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // Ignore save errors to avoid crashing the overlay.
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string GetVirtualKeyLabel(int virtualKey)
    {
        return virtualKey switch
        {
            0xC0 => "`",
            0x70 => "F1",
            0x71 => "F2",
            0x72 => "F3",
            0x73 => "F4",
            0x74 => "F5",
            0x75 => "F6",
            0x76 => "F7",
            0x77 => "F8",
            0x78 => "F9",
            0x79 => "F10",
            0x7A => "F11",
            0x7B => "F12",
            _ => ((System.Windows.Input.Key)virtualKey).ToString()
        };
    }
}
