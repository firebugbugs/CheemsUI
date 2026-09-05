using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>
/// Uiverse.io（作者 csemszepp）立方体拼贴静态背景的原生 WPF 绘制层。
/// <para>
/// 原版是四层全部由硬色标组成的 conic-gradient，按 150px 平铺：
/// 底层 <c>from -45deg</c>（c1 顶部 90°、c2 右下 135°、c4 左下 135°），
/// 其上三层分别在瓦片的 50%100%、25%25%、75%75% 中心叠加 c3/c1 扇形。
/// 因为没有任何平滑过渡，每层都能转换为角度精确的实心扇形几何，
/// 合成结果与浏览器逐像素一致（仅剩抗锯齿差异）。
/// </para>
/// <para>
/// 源码 CSS 的 <c>--s</c>（瓦片尺寸）对应 <see cref="TileSize"/>；
/// 四个颜色保持源码原值，不提供改色选项。
/// </para>
/// </summary>
public sealed class CheemsCubesBackground : FrameworkElement
{
    // --c1 / --c2 / --c3 / --c4，逐值取自源码 CSS。
    private static readonly Color Salmon = Color.FromRgb(0xFF, 0x84, 0x7C);
    private static readonly Color Crimson = Color.FromRgb(0xE8, 0x4A, 0x5F);
    private static readonly Color Peach = Color.FromRgb(0xFE, 0xCE, 0xA8);
    private static readonly Color Sage = Color.FromRgb(0x99, 0xB8, 0x98);

    private DrawingBrush? _tileBrush;

    /// <summary>预览卡模式下点击自身时触发，由 Demo 转发给窗口应用背景。</summary>
    public event EventHandler? ApplyRequested;

    public static readonly DependencyProperty TileSizeProperty = DependencyProperty.Register(
        nameof(TileSize), typeof(double), typeof(CheemsCubesBackground),
        new PropertyMetadata(150d, OnTileSizeChanged, CoerceTileSize));

    public static readonly DependencyProperty IsPreviewProperty = DependencyProperty.Register(
        nameof(IsPreview), typeof(bool), typeof(CheemsCubesBackground), new PropertyMetadata(false));

    public static readonly DependencyProperty ClipRadiusProperty = DependencyProperty.Register(
        nameof(ClipRadius), typeof(double), typeof(CheemsCubesBackground),
        new PropertyMetadata(0d, OnClipRadiusChanged));

    public CheemsCubesBackground()
    {
        Cursor = Cursors.Hand;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        SizeChanged += (_, _) => UpdateClip();
    }

    /// <summary>源码 CSS 变量 --s：单个瓦片的边长（设备无关像素）。</summary>
    public double TileSize { get => (double)GetValue(TileSizeProperty); set => SetValue(TileSizeProperty, value); }

    /// <summary>是否处于卡片预览模式：仅此模式下点击会触发 <see cref="ApplyRequested"/>。</summary>
    public bool IsPreview { get => (bool)GetValue(IsPreviewProperty); set => SetValue(IsPreviewProperty, value); }

    /// <summary>预览卡的圆角半径，用于裁剪四角。</summary>
    public double ClipRadius { get => (double)GetValue(ClipRadiusProperty); set => SetValue(ClipRadiusProperty, value); }

    private static object CoerceTileSize(DependencyObject d, object baseValue) =>
        baseValue is double size && size < 1d ? 1d : baseValue;

    private static void OnTileSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CheemsCubesBackground)d;
        control._tileBrush = null;
        control.InvalidateVisual();
    }

    private static void OnClipRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((CheemsCubesBackground)d).UpdateClip();

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsPreview)
        {
            ApplyRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        _tileBrush ??= CreateTileBrush(TileSize);
        drawingContext.DrawRectangle(_tileBrush, null, new Rect(0, 0, ActualWidth, ActualHeight));
    }

    private void UpdateClip()
    {
        Clip = ClipRadius > 0 && ActualWidth > 0 && ActualHeight > 0
            ? new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight), ClipRadius, ClipRadius)
            : null;
    }

    /// <summary>
    /// 构建单个瓦片的绘制内容并包装为按 <see cref="Viewport"/> 无限平铺的画刷。
    /// 角度为 CSS conic 语义：0° 指向上方，顺时针为正。
    /// </summary>
    private static DrawingBrush CreateTileBrush(double size)
    {
        var group = new DrawingGroup();
        using (var context = group.Open())
        {
            // conic 的扇形会伸出瓦片边界，靠这一层裁剪保证背景定位区域与 CSS background-size 一致。
            context.PushClip(new RectangleGeometry(new Rect(0, 0, size, size)));
            var radius = size * 1.5;
            var half = size / 2;

            // 底层：conic-gradient(from -45deg, c1 90deg, c2 0 225deg, c4 0)，中心 50% 50%。
            // 先用 c2（绝对角 45°–180°）整幅铺底，再按顺时针顺序覆盖其余两个扇区；
            // 后绘制扇形的抗锯齿边缘始终落在真实相邻色块上，与浏览器硬色标边缘一致。
            context.DrawRectangle(CreateFrozenBrush(Crimson), null, new Rect(0, 0, size, size));
            context.DrawGeometry(CreateFrozenBrush(Salmon), null, CreateSector(new Point(half, half), radius, -45, 45));
            context.DrawGeometry(CreateFrozenBrush(Sage), null, CreateSector(new Point(half, half), radius, 180, 315));

            // 第三层：conic-gradient(from -45deg at 50% 100%, #0000 180deg, c3 0)。
            context.DrawGeometry(CreateFrozenBrush(Peach), null, CreateSector(new Point(half, size), radius, 135, 315));

            // 第二层：conic-gradient(from -45deg at 25% 25%, c3 90deg, #0000 0)。
            context.DrawGeometry(CreateFrozenBrush(Peach), null, CreateSector(new Point(size * 0.25, size * 0.25), radius, -45, 45));

            // 顶层：conic-gradient(from 45deg at 75% 75%, c3 90deg, c1 0 180deg, #0000 0)。
            // 两段相邻，先画 c3 再画 c1，让 c1 的边缘抗锯齿叠在 c3 上。
            context.DrawGeometry(CreateFrozenBrush(Peach), null, CreateSector(new Point(size * 0.75, size * 0.75), radius, 45, 135));
            context.DrawGeometry(CreateFrozenBrush(Salmon), null, CreateSector(new Point(size * 0.75, size * 0.75), radius, 135, 225));

            context.Pop();
        }

        group.Freeze();
        var brush = new DrawingBrush(group)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, size, size),
            ViewportUnits = BrushMappingMode.Absolute,
            Stretch = Stretch.None
        };
        brush.Freeze();
        return brush;
    }

    private static Brush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    /// <summary>以 center 为顶点、从 startDegrees 顺时针扫到 endDegrees 的扇形（半径足够覆盖整个瓦片）。</summary>
    private static StreamGeometry CreateSector(Point center, double radius, double startDegrees, double endDegrees)
    {
        Point Polar(double degrees)
        {
            var radians = degrees * Math.PI / 180;
            return new Point(center.X + Math.Sin(radians) * radius, center.Y - Math.Cos(radians) * radius);
        }

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(center, true, true);
            context.LineTo(Polar(startDegrees), true, false);
            context.ArcTo(
                Polar(endDegrees),
                new Size(radius, radius),
                0d,
                endDegrees - startDegrees > 180d,
                SweepDirection.Clockwise,
                true,
                false);
        }

        geometry.Freeze();
        return geometry;
    }
}
