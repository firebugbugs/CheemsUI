using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CheemsControl.App.Infrastructure;

/// <summary>
/// 把待录制控件连接到屏幕外的透明 PresentationSource，使 Loaded、模板和动画正常运行。
/// </summary>
internal sealed class GifCaptureHost : IDisposable
{
    private const double CapturePadding = 40;

    private readonly Border _captureRoot;
    private readonly Window _window;
    private bool _disposed;

    public GifCaptureHost(Control control)
    {
        _captureRoot = new Border
        {
            Padding = new Thickness(CapturePadding),
            Background = Brushes.Transparent,
            Child = control,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            UseLayoutRounding = true,
            SnapsToDevicePixels = false
        };

        _window = new Window
        {
            Content = _captureRoot,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            ShowActivated = false,
            ShowInTaskbar = false,
            Left = SystemParameters.VirtualScreenLeft - 10000,
            Top = SystemParameters.VirtualScreenTop - 10000,
            Topmost = false
        };

        var owner = Application.Current?.MainWindow;
        if (owner is { IsLoaded: true })
        {
            _window.Owner = owner;
        }
    }

    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _window.Show();

        await _window.Dispatcher.InvokeAsync(
            () => { },
            DispatcherPriority.Loaded,
            cancellationToken);
        await PrepareFrameAsync(cancellationToken);
    }

    public async Task PrepareFrameAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _captureRoot.UpdateLayout();
        await _window.Dispatcher.InvokeAsync(
            () => _captureRoot.UpdateLayout(),
            DispatcherPriority.Render,
            cancellationToken);
    }

    public BitmapSource Capture()
    {
        var pixelWidth = Math.Max(1, (int)Math.Ceiling(_captureRoot.ActualWidth));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(_captureRoot.ActualHeight));
        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(_captureRoot);
        bitmap.Freeze();
        return bitmap;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _captureRoot.Child = null;
        _window.Close();
    }
}
