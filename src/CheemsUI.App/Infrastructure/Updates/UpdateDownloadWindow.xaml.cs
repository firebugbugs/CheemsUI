using System.Globalization;
using System.Windows;

namespace CheemsUI.App.Infrastructure.Updates;

internal partial class UpdateDownloadWindow : Window
{
    private readonly UpdateService _updateService;
    private readonly UpdateRelease _release;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _completed;

    public UpdateDownloadWindow(Window owner, UpdateService updateService, UpdateRelease release)
    {
        Owner = owner;
        _updateService = updateService;
        _release = release;
        InitializeComponent();
        PartVersion.Text = $"CheemsUI {release.Version}";
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            PartStatus.Text = "正在下载更新包…";
            var progress = new Progress<UpdateDownloadProgress>(UpdateProgress);
            var installerPath = await _updateService.DownloadInstallerAsync(_release, progress, _cancellationTokenSource.Token);
            _completed = true;
            PartProgress.Value = 100;
            PartStatus.Text = "下载并校验完成，正在启动安装程序…";
            PartDetail.Text = "程序将自动关闭，安装完成后可从开始菜单启动新版本。";
            PartCancelButton.IsEnabled = false;

            UpdateInstallerLauncher.LaunchInstallerAfterExit(installerPath);
            Application.Current.Shutdown();
        }
        catch (OperationCanceledException)
        {
            PartStatus.Text = "下载已取消。";
            PartDetail.Text = string.Empty;
            PartCancelButton.Content = "关闭";
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex);
            PartStatus.Text = "更新下载失败。";
            PartDetail.Text = ex.Message;
            PartCancelButton.Content = "关闭";
        }
    }

    private void UpdateProgress(UpdateDownloadProgress progress)
    {
        PartProgress.IsIndeterminate = progress.TotalBytes is null;
        PartProgress.Value = progress.Percentage;
        PartDetail.Text = progress.TotalBytes is > 0
            ? $"{FormatSize(progress.DownloadedBytes)} / {FormatSize(progress.TotalBytes.Value)}（{progress.Percentage:0}%）"
            : $"已下载 {FormatSize(progress.DownloadedBytes)}";
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_completed && !_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
            PartCancelButton.IsEnabled = false;
            return;
        }

        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        base.OnClosed(e);
    }

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value.ToString(unit == 0 ? "0" : "0.0", CultureInfo.InvariantCulture)} {units[unit]}";
    }
}
