using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace NumpadOverlay;

public partial class UpdateWindow : Window
{
    private readonly string _latestVersion;
    private readonly JsonElement _assets;
    private bool _isUpdating;

    public UpdateWindow(string currentVersion, string latestVersion, string changelog, JsonElement assets)
    {
        InitializeComponent();
        _latestVersion = latestVersion;
        _assets = assets;
        VersionText.Text = $"Phiên bản hiện tại: {currentVersion} → {latestVersion}";
        ChangelogText.Text = string.IsNullOrWhiteSpace(changelog) ? "Không có changelog." : changelog;
        StatusText.Text = "Sẵn sàng cập nhật";
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        UpdateButton.IsEnabled = false;
        StatusText.Text = "Đang tải bản cập nhật...";

        try
        {
            await DownloadAndApplyAsync();
        }
        catch
        {
            StatusText.Text = "Lỗi cập nhật";
            UpdateButton.IsEnabled = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async Task DownloadAndApplyAsync()
    {
        try
        {
            string? url = null;
        foreach (var a in _assets.EnumerateArray())
        {
            var name = a.GetProperty("name").GetString() ?? "";
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                url = a.GetProperty("browser_download_url").GetString();
                break;
            }
        }
        if (url == null) throw new Exception("No asset");

        var temp = Path.Combine(Path.GetTempPath(), "OpenNP_Update");
        Directory.CreateDirectory(temp);
        var file = Path.Combine(temp, Path.GetFileName(url));

        using var client = new HttpClient();
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        var total = response.Content.Headers.ContentLength ?? 0;
        var stream = await response.Content.ReadAsStreamAsync();

        await using var fs = File.Create(file);
        var buffer = new byte[81920];
        long read = 0;
        int bytes;
        while ((bytes = await stream.ReadAsync(buffer)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, bytes));
            read += bytes;
            if (total > 0)
                DownloadProgress.Value = (int)(read * 100 / total);
        }

        StatusText.Text = "Đang giải nén...";

        string exePath;
        if (file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            var extract = Path.Combine(temp, "extract");
            ZipFile.ExtractToDirectory(file, extract, true);
            exePath = Directory.GetFiles(extract, "OpenNP.exe", SearchOption.AllDirectories)[0];
        }
        else
        {
            exePath = file;
        }

        Process.Start(exePath);
        // cleanup temp
        try { Directory.Delete(temp, true); } catch { }
        System.Windows.Application.Current.Shutdown();
        }
        catch
        {
            StatusText.Text = "Lỗi cập nhật (giữ bản cũ)";
            UpdateButton.IsEnabled = true;
            _isUpdating = false;
        }
    }
}