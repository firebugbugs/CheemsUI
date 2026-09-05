using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>
/// Uiverse.io（作者 whoisyourdeadie）Matrix 数字雨动态背景的原生 WPF 绘制层。
/// <para>
/// 原版是纯 CSS 动画：200 个 20px 宽的字形列按 25px 间距排布（5 组 pattern × 40 列），
/// 每列以各自的负延迟与时长循环执行 fall（translateY -10% → 200%、opacity 1 → 0、linear），
/// 字符串通过 background-clip:text 填充“白头 → 亮绿 → 暗绿 → 透明”的纵向渐变。
/// </para>
/// <para>
/// WPF 侧按浏览器实测值逐项还原：25px 列距、20px 列宽、16px 粗体字形、
/// 字形中心位于列内 x=9px（line-height 18px 行盒）、字符垂直步进 = 字形自然步进 + 1px letter-spacing，
/// 渐变按整条字符串高度采样到每个字符。列按控件宽度生成，超过原版 5000px 时按 40 列周期延续。
/// 源码的视口媒体查询（≤768/480px 缩小字号）是浏览器视口规则，不转换为控件尺寸逻辑。
/// </para>
/// </summary>
public sealed class CheemsMatrixRainBackground : FrameworkElement
{
    private const double ColumnPitch = 25;
    private const double GlyphCenterX = 9;
    private const double GlyphSize = 16;
    private const double LetterSpacing = 1;
    private const int ColumnsPerPattern = 40;

    // 预览卡静默运行零点几秒后触发 PreviewFrozen，让宿主换成静态快照；负延迟使 t=0 时各列
    // 已处于不同下落相位，首帧即具代表性，无需播放多个周期（与其他背景卡的静默冻结节奏一致）。
    private const double PreviewFreezeSeconds = 0.4;

    // 源码未指定 font-family（浏览器默认无衬线）。实测各系统日文字体的假名步进：
    // Yu Gothic = 16.00px（全角 1em，与浏览器日文回退一致），Meiryo/Meiryo UI/Yu Gothic UI ≈ 12.7px
    // （比例宽度，会破坏原版 17px 的垂直节奏），MS Gothic = 16.31px。故 Yu Gothic 优先。
    private const string GlyphFontFamily = "Yu Gothic, Yu Gothic UI, Meiryo, Segoe UI";

    // nth-child(1..40) 的 animation-delay 与 animation-duration，逐值迁移自源码。
    private static readonly double[] ColumnDelays =
    {
        -2.5, -3.2, -1.8, -2.9, -1.5, -3.8, -2.1, -2.7, -3.4, -1.9,
        -3.6, -2.3, -3.1, -2.6, -3.7, -2.8, -3.3, -2.2, -3.9, -2.4,
        -1.7, -3.5, -2.0, -4.0, -1.6, -3.0, -3.8, -2.5, -3.2, -2.7,
        -1.8, -3.6, -2.1, -3.4, -2.8, -3.7, -2.3, -1.9, -3.5, -2.6
    };
    private static readonly double[] ColumnDurations =
    {
        3.0, 4.0, 2.5, 3.5, 3.0, 4.5, 2.8, 3.2, 3.8, 2.7,
        4.2, 3.1, 3.6, 2.9, 4.1, 3.3, 3.7, 2.6, 4.3, 3.4,
        2.4, 3.9, 3.0, 4.4, 2.3, 3.5, 4.0, 2.8, 3.6, 3.2,
        2.7, 4.1, 3.1, 3.7, 2.9, 4.2, 3.3, 2.5, 3.8, 3.4
    };

    // 字符集级联（源码后声明覆盖先声明）：n%5 → n%4 → n%3 → even → odd。
    private static readonly string[] CascadeStrings =
    {
        "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン123456789",
        "ガギグゲゴザジズゼゾダヂヅデドバビブベボパピプペポヴァィゥェォャュョッABCDEFGHIJKLMNOPQRSTUVWXYZ",
        "アカサタナハマヤラワイキシチニヒミリウクスツヌフムユルエケセテネヘメレオコソトノホモヨロヲン0987654321",
        "ンヲロヨモホノトソコオレメヘネテセケエルユムフヌツスクウリミヒニチシキイワラヤマハナタサカア",
        "ガザダバパギジヂビピグズヅブプゲゼデベペゴゾドボポヴァィゥェォャュョッ!@#$%^&*()_+-=[]{}|;:,.<>?"
    };

    // ::before 的 linear-gradient(to bottom, ...) 色标，位置 = 字符中心 / 字符串总高。
    // CSS 渐变在预乘空间插值；本组色标逐通道线性插值与其等价（含 90%→100% 的透明段）。
    private static readonly (double Offset, Color Color)[] GradientStops =
    {
        (0.00, Color.FromRgb(0xFF, 0xFF, 0xFF)),
        (0.05, Color.FromRgb(0xFF, 0xFF, 0xFF)),
        (0.10, Color.FromRgb(0x00, 0xFF, 0x41)),
        (0.20, Color.FromRgb(0x00, 0xFF, 0x41)),
        (0.30, Color.FromRgb(0x00, 0xDD, 0x33)),
        (0.40, Color.FromRgb(0x00, 0xBB, 0x22)),
        (0.50, Color.FromRgb(0x00, 0x99, 0x11)),
        (0.60, Color.FromRgb(0x00, 0x77, 0x00)),
        (0.70, Color.FromRgb(0x00, 0x55, 0x00)),
        (0.80, Color.FromRgb(0x00, 0x33, 0x00)),
        (0.90, Color.FromArgb(0x80, 0x00, 0xFF, 0x41)),
        (1.00, Color.FromArgb(0x00, 0x00, 0x00, 0x00))
    };

    private static readonly Brush BackdropBrush = CreateFrozenBrush(Color.FromRgb(0, 0, 0));

    // 每条字符串的预渲染图层（按字符串索引缓存，冻结后可跨实例共享）。
    private static readonly Dictionary<int, (Drawing Drawing, double Height)?> StringDrawings = new();

    private readonly List<(int N, double Left, int StringIndex)> _columns = new();
    private double _animationSeconds;
    private long _lastTickTimestamp;
    private bool _renderingSubscribed;
    private bool _previewFrozenRaised;

    /// <summary>预览卡模式下点击自身时触发，由 Demo 转发给窗口应用背景。</summary>
    public event EventHandler? ApplyRequested;

    /// <summary>
    /// 预览卡模式下播放一小段后触发一次，宿主可据此把卡片换成静态快照以节省资源
    ///（与其他背景卡的“静态预览冻结”管线一致）。
    /// </summary>
    public event EventHandler? PreviewFrozen;

    public static readonly DependencyProperty IsPreviewProperty = DependencyProperty.Register(
        nameof(IsPreview), typeof(bool), typeof(CheemsMatrixRainBackground), new PropertyMetadata(false));

    public static readonly DependencyProperty ClipRadiusProperty = DependencyProperty.Register(
        nameof(ClipRadius), typeof(double), typeof(CheemsMatrixRainBackground),
        new PropertyMetadata(0d, OnClipRadiusChanged));

    public static readonly DependencyProperty AnimationSpeedProperty = DependencyProperty.Register(
        nameof(AnimationSpeed), typeof(double), typeof(CheemsMatrixRainBackground),
        new PropertyMetadata(1d, OnOptionChanged));

    public static readonly DependencyProperty IsAnimationEnabledProperty = DependencyProperty.Register(
        nameof(IsAnimationEnabled), typeof(bool), typeof(CheemsMatrixRainBackground),
        new PropertyMetadata(true, OnOptionChanged));

    public CheemsMatrixRainBackground()
    {
        Cursor = Cursors.Hand;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        SizeChanged += (_, _) =>
        {
            RebuildColumns();
            UpdateClip();
        };
    }

    /// <summary>是否处于卡片预览模式：仅此模式下点击会触发 <see cref="ApplyRequested"/>。</summary>
    public bool IsPreview { get => (bool)GetValue(IsPreviewProperty); set => SetValue(IsPreviewProperty, value); }

    /// <summary>预览卡的圆角半径，用于裁剪四角。</summary>
    public double ClipRadius { get => (double)GetValue(ClipRadiusProperty); set => SetValue(ClipRadiusProperty, value); }

    /// <summary>动画速度倍率（作用于 fall 循环时钟）。</summary>
    public double AnimationSpeed { get => (double)GetValue(AnimationSpeedProperty); set => SetValue(AnimationSpeedProperty, value); }

    /// <summary>为假时冻结当前画面（数字雨停在当前帧）。</summary>
    public bool IsAnimationEnabled { get => (bool)GetValue(IsAnimationEnabledProperty); set => SetValue(IsAnimationEnabledProperty, value); }

    private static void OnClipRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((CheemsMatrixRainBackground)d).UpdateClip();

    private static void OnOptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((CheemsMatrixRainBackground)d).InvalidateVisual();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _lastTickTimestamp = Stopwatch.GetTimestamp();
        RebuildColumns();
        UpdateClip();
        SubscribeRendering();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => UnsubscribeRendering();

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsPreview)
        {
            ApplyRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SubscribeRendering()
    {
        if (_renderingSubscribed)
        {
            return;
        }

        _renderingSubscribed = true;
        CompositionTarget.Rendering += OnRendering;
    }

    private void UnsubscribeRendering()
    {
        if (!_renderingSubscribed)
        {
            return;
        }

        _renderingSubscribed = false;
        CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = Stopwatch.GetTimestamp();
        // 时间戳始终前进；不可见或暂停时不累计动画时钟，恢复后从冻结相位继续。
        if (IsVisible && IsAnimationEnabled && _lastTickTimestamp != 0)
        {
            _animationSeconds += (now - _lastTickTimestamp) / (double)Stopwatch.Frequency * Math.Max(AnimationSpeed, 0);
        }

        _lastTickTimestamp = now;
        if (IsVisible && IsAnimationEnabled)
        {
            InvalidateVisual();
        }

        if (IsPreview && !_previewFrozenRaised && _animationSeconds >= PreviewFreezeSeconds)
        {
            _previewFrozenRaised = true;
            // 渲染回调中不直接改视觉树，转交 dispatcher 后由宿主换静态快照。
            Dispatcher.BeginInvoke(() => PreviewFrozen?.Invoke(this, EventArgs.Empty));
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        drawingContext.DrawRectangle(BackdropBrush, null, new Rect(0, 0, ActualWidth, ActualHeight));

        foreach (var column in _columns)
        {
            var (drawing, stringHeight) = GetStringDrawing(column.StringIndex);
            var delay = ColumnDelays[column.N - 1];
            var duration = ColumnDurations[column.N - 1];
            var progress = ((_animationSeconds - delay) / duration) % 1d;
            if (progress < 0)
            {
                progress += 1d;
            }

            // CSS：top:-100% + translateY(-10% → 200%)，百分比均取容器高度；opacity 1 → 0。
            var top = -1.1 * ActualHeight + progress * 2.1 * ActualHeight;
            var opacity = 1d - progress;
            if (opacity <= 0 || top >= ActualHeight || top + stringHeight <= 0)
            {
                continue;
            }

            drawingContext.PushOpacity(opacity);
            drawingContext.PushTransform(new TranslateTransform(column.Left, top));
            drawingContext.DrawDrawing(drawing);
            drawingContext.Pop();
            drawingContext.Pop();
        }
    }

    private void UpdateClip()
    {
        Clip = ClipRadius > 0 && ActualWidth > 0 && ActualHeight > 0
            ? new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight), ClipRadius, ClipRadius)
            : null;
    }

    private void RebuildColumns()
    {
        _columns.Clear();
        var count = (int)Math.Floor((ActualWidth - 1) / ColumnPitch) + 1;
        for (var k = 0; k < count; k++)
        {
            var n = k % ColumnsPerPattern + 1;
            _columns.Add((n, k * ColumnPitch, StringIndexFor(n)));
        }
    }

    private static int StringIndexFor(int n) =>
        n % 5 == 0 ? 4 :
        n % 4 == 0 ? 3 :
        n % 3 == 0 ? 2 :
        n % 2 == 0 ? 1 : 0;

    /// <summary>按字符串索引懒加载预渲染图层：每字符按其在串中的中心位置采样渐变纯色。</summary>
    private static (Drawing Drawing, double Height) GetStringDrawing(int stringIndex)
    {
        if (StringDrawings.TryGetValue(stringIndex, out var cached) && cached is { } cachedValue)
        {
            return cachedValue;
        }

        var text = CascadeStrings[stringIndex];
        var typeface = new Typeface(new FontFamily(GlyphFontFamily), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        var culture = CultureInfo.InvariantCulture;
        var advances = new double[text.Length];
        var totalHeight = 0d;
        for (var i = 0; i < text.Length; i++)
        {
            var glyph = new FormattedText(
                text[i].ToString(), culture, FlowDirection.LeftToRight,
                typeface, GlyphSize, Brushes.White, 1d);
            advances[i] = glyph.Width;
            totalHeight += advances[i] + LetterSpacing;
        }

        var group = new DrawingGroup();
        using (var context = group.Open())
        {
            var prefix = 0d;
            for (var i = 0; i < text.Length; i++)
            {
                var cellSize = advances[i] + LetterSpacing;
                var gradientPosition = (prefix + advances[i] / 2) / totalHeight;
                // 颜色必须在构造时传入：DrawText 之后调用 SetForegroundBrush 不会改变已绘制的字形颜色。
                var glyph = new FormattedText(
                    text[i].ToString(), culture, FlowDirection.LeftToRight,
                    typeface, GlyphSize, CreateFrozenBrush(SampleGradient(gradientPosition)), 1d);
                // 字形在垂直书写单元内居中：水平中心对齐列内 x=9px，垂直按单元尺寸居中。
                context.DrawText(glyph, new Point(GlyphCenterX - glyph.Width / 2, prefix + (cellSize - glyph.Height) / 2));
                prefix += cellSize;
            }
        }

        group.Freeze();
        var value = ((Drawing Drawing, double Height))(group, totalHeight);
        StringDrawings[stringIndex] = value;
        return value;
    }

    private static Color SampleGradient(double position)
    {
        if (position <= GradientStops[0].Offset)
        {
            return GradientStops[0].Color;
        }

        for (var i = 1; i < GradientStops.Length; i++)
        {
            if (position > GradientStops[i].Offset)
            {
                continue;
            }

            var (leftOffset, leftColor) = GradientStops[i - 1];
            var (rightOffset, rightColor) = GradientStops[i];
            var progress = (position - leftOffset) / (rightOffset - leftOffset);
            return Color.FromArgb(
                LerpChannel(leftColor.A, rightColor.A, progress),
                LerpChannel(leftColor.R, rightColor.R, progress),
                LerpChannel(leftColor.G, rightColor.G, progress),
                LerpChannel(leftColor.B, rightColor.B, progress));
        }

        return GradientStops[^1].Color;
    }

    private static byte LerpChannel(byte from, byte to, double progress) =>
        (byte)Math.Round(from + (to - from) * progress, MidpointRounding.AwayFromZero);

    private static Brush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
