using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CheemsControl.App.Infrastructure;

/// <summary>
/// 创建一个置顶的可见录制窗口，控件在其中运行。
/// </summary>
internal sealed class GifCaptureHost : IDisposable
{
    private readonly Control _control;
    private readonly Border _captureRoot;
    private readonly Window _window;
    private readonly double _expandPercent;
    private readonly Rect? _motionBounds;
    private bool _disposed;

    private const double MaxStageWidth = 800;
    private const double MaxStageHeight = 600;
    private const double SlotWidth = MaxStageWidth + 20;
    private const double SlotHeight = MaxStageHeight + 20;

    /// <summary>运动边界外的额外留白比例（与原有 Loader +10% 边距语义一致）。</summary>
    private const double MotionPaddingPercent = 10;

    /// <summary>屏幕工作区能容纳的互不重叠录制窗口槽位数（并行录制的上限）。</summary>
    public static int MaxSlots
    {
        get
        {
            var work = SystemParameters.WorkArea;
            var cols = Math.Max(1, (int)(work.Width / SlotWidth));
            var rows = Math.Max(1, (int)(work.Height / SlotHeight));
            return cols * rows;
        }
    }

    /// <summary>槽位号换算为屏幕上的窗口左上角，保证并行窗口互不遮挡（截图走 BitBlt，遮挡会互相污染）。</summary>
    public static Point GetSlotOrigin(int slot)
    {
        var work = SystemParameters.WorkArea;
        var cols = Math.Max(1, (int)(work.Width / SlotWidth));
        var rows = Math.Max(1, (int)(work.Height / SlotHeight));
        var col = slot % cols;
        var row = (slot / cols) % rows;
        return new Point(work.Left + col * SlotWidth, work.Top + row * SlotHeight);
    }

    public GifCaptureHost(Control control, double expandPercent = 0, Point? position = null, Rect? motionBounds = null)
    {
        _control = control;
        _expandPercent = expandPercent;
        _motionBounds = motionBounds;

        control.HorizontalAlignment = HorizontalAlignment.Center;
        control.VerticalAlignment = VerticalAlignment.Center;

        // 与 DemoArea 展示台同色（#E8E8E8）：纯白底会让 BounceBall 这类白色控件隐形
        var stageBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8));
        stageBrush.Freeze();

        _captureRoot = new Border
        {
            Padding = new Thickness(20),
            Background = stageBrush,
            Child = control,
            UseLayoutRounding = true
        };

        var origin = position ?? new Point(100, 100);
        _window = new Window
        {
            Title = "GIF Recording",
            Content = _captureRoot,
            Background = stageBrush,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            ShowActivated = false,
            ShowInTaskbar = true,
            Topmost = true,
            Left = origin.X,
            Top = origin.Y
        };
    }

    public async Task OpenAsync(CancellationToken ct)
    {
        _window.Show();

        // 等待窗口 Loaded 事件（确保句柄创建完成）
        if (!IsWindowReady())
        {
            var tcs = new TaskCompletionSource();
            _window.Loaded += (_, _) => tcs.TrySetResult();
            await tcs.Task;
        }

        _window.UpdateLayout();
        ApplyBoundedStageSize();

        // 如果窗口尺寸太小，手动设置
        var hwnd = new WindowInteropHelper(_window).Handle;
        GetWindowRect(hwnd, out var rect);
        if ((rect.Right - rect.Left) < 10)
        {
            _captureRoot.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var dw = _captureRoot.DesiredSize.Width;
            var dh = _captureRoot.DesiredSize.Height;
            if (dw > 1 && dh > 1)
            {
                _window.SizeToContent = SizeToContent.Manual;
                _window.Width = dw;
                _window.Height = dh;
                _window.UpdateLayout();
            }
        }

        if (_motionBounds is { } bounds && !bounds.IsEmpty)
        {
            ApplyMotionStage(bounds);
        }
        else if (_expandPercent > 0)
        {
            var eh = _control.ActualWidth * _expandPercent / 100.0;
            var ev = _control.ActualHeight * _expandPercent / 100.0;
            _captureRoot.Padding = new Thickness(
                Math.Max(20, eh), Math.Max(20, ev),
                Math.Max(20, eh), Math.Max(20, ev));
            _window.UpdateLayout();
        }
    }

    /// <summary>
    /// 按动画全程的渲染边界布置舞台：控件以初始尺寸放入固定大小的画布，
    /// 四周留出运动越界量 + 10% 边距。录制窗口尺寸全程稳定，
    /// 运动超出初始尺寸的部分（如 NewtonsCradle 摆球）不会被裁切。
    /// </summary>
    private void ApplyMotionStage(Rect bounds)
    {
        var controlWidth = _control.ActualWidth;
        var controlHeight = _control.ActualHeight;
        if (controlWidth <= 0 || controlHeight <= 0)
        {
            return;
        }

        var padH = Math.Max(20, bounds.Width * MotionPaddingPercent / 100.0);
        var padV = Math.Max(20, bounds.Height * MotionPaddingPercent / 100.0);
        var left = Math.Max(0, -bounds.Left) + padH;
        var top = Math.Max(0, -bounds.Top) + padV;
        var right = Math.Max(0, bounds.Right - controlWidth) + padH;
        var bottom = Math.Max(0, bounds.Bottom - controlHeight) + padV;

        // 尊重舞台上限：并行录制槽位互不重叠的前提
        var width = Math.Min(controlWidth + left + right, MaxStageWidth);
        var height = Math.Min(controlHeight + top + bottom, MaxStageHeight);

        var canvas = new Canvas { Width = width, Height = height };
        _captureRoot.Padding = new Thickness(0);
        _captureRoot.Child = canvas;
        canvas.Children.Add(_control);
        _control.HorizontalAlignment = HorizontalAlignment.Left;
        _control.VerticalAlignment = VerticalAlignment.Top;
        Canvas.SetLeft(_control, Math.Min(left, width - controlWidth));
        Canvas.SetTop(_control, Math.Min(top, height - controlHeight));
        _window.UpdateLayout();
    }

    /// <summary>控件当前渲染边界（含所有视觉子元素，坐标相对控件原点，可为负）。</summary>
    public Rect GetRenderBounds()
    {
        var bounds = VisualTreeHelper.GetDescendantBounds(_control);
        bounds.Union(new Rect(0, 0,
            Math.Max(_control.ActualWidth, _control.DesiredSize.Width),
            Math.Max(_control.ActualHeight, _control.DesiredSize.Height)));
        return bounds;
    }

    private bool IsWindowReady()
    {
        var hwnd = new WindowInteropHelper(_window).Handle;
        return hwnd != IntPtr.Zero;
    }

    /// <summary>
    /// SizeToContent 实际以屏幕量级的有限约束测量内容；模板根为 Viewbox 且无显式宽高的控件
    /// 会按 Viewbox 默认行为放大填满约束，撑出全屏窗口。
    /// 统一规则：对比无限约束下的自然尺寸，控件尺寸若随约束放大则固定为自然尺寸（钳制到舞台上限），
    /// 不依赖任何具体控件类型，未来新增同类控件自动生效。
    /// </summary>
    private void ApplyBoundedStageSize()
    {
        _control.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var natural = _control.DesiredSize;
        _control.Measure(new Size(MaxStageWidth, MaxStageHeight));
        var bounded = _control.DesiredSize;

        var pinned = false;
        if (double.IsNaN(_control.Width) && bounded.Width > natural.Width + 1)
        {
            _control.Width = Math.Min(natural.Width, MaxStageWidth);
            pinned = true;
        }

        if (double.IsNaN(_control.Height) && bounded.Height > natural.Height + 1)
        {
            _control.Height = Math.Min(natural.Height, MaxStageHeight);
            pinned = true;
        }

        if (pinned)
        {
            _window.UpdateLayout();
        }
    }

    public void PrepareFrame()
    {
        _captureRoot.UpdateLayout();
    }

    public Size GetControlSize()
    {
        return new Size(_control.ActualWidth, _control.ActualHeight);
    }

    public BitmapSource Capture()
    {
        _captureRoot.UpdateLayout();

        // UpdateLayout 只做布局，实际绘制排在 Dispatcher 的 Render 优先级队列里；
        // 同步排空该队列，保证 BitBlt 读到的是本帧画面而不是上一次绘制的结果。
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Render);

        var hwnd = new WindowInteropHelper(_window).Handle;
        GetWindowRect(hwnd, out var rect);
        var w = Math.Max(1, rect.Right - rect.Left);
        var h = Math.Max(1, rect.Bottom - rect.Top);

        var hdcSrc = GetWindowDC(hwnd);
        var hdcMem = CreateCompatibleDC(hdcSrc);
        var hBmp = CreateCompatibleBitmap(hdcSrc, w, h);
        var hOld = SelectObject(hdcMem, hBmp);

        BitBlt(hdcMem, 0, 0, w, h, hdcSrc, 0, 0, SRCCOPY);

        SelectObject(hdcMem, hOld);
        DeleteDC(hdcMem);
        ReleaseDC(hwnd, hdcSrc);

        var bitmap = Imaging.CreateBitmapSourceFromHBitmap(
            hBmp, IntPtr.Zero, new Int32Rect(0, 0, w, h), BitmapSizeOptions.FromEmptyOptions());
        bitmap.Freeze();
        DeleteObject(hBmp);
        return bitmap;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _captureRoot.Child = null;
        _window.Close();
    }

    private const int SRCCOPY = 0x00CC0020;

    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowDC(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSrc, int xSrc, int ySrc, int rop);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }
}
