using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CheemsUI.App.Infrastructure;

/// <summary>
/// App-only sidebar branding inspired by the Uiverse Smit-Prajapati card logo.
/// The collapsed mark shows "UI"; hovering reveals "CheemsUI" and cheems.cn.
/// </summary>
public sealed class SidebarBrandControl : Control
{
    private const double LogoDurationSeconds = 0.5;
    private const double CaptionDelaySeconds = 0.25;
    private const double CaptionDurationSeconds = 0.25;

    private static readonly Brush GoldBrush = CreateFrozenBrush(Color.FromRgb(0xBD, 0x9F, 0x67));
    private static readonly Pen GoldOutlinePen = CreateFrozenPen(GoldBrush, 1.0);

    private double _logoProgress;
    private double _captionProgress;
    private double _logoStart;
    private double _captionStart;
    private double _target;
    private long _transitionStartedAt;
    private bool _isRendering;

    public SidebarBrandControl()
    {
        Focusable = false;
        IsTabStop = false;
        Background = Brushes.Transparent;
        SnapsToDevicePixels = true;

        Unloaded += OnUnloaded;
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        BeginTransition(1.0);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        BeginTransition(0.0);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        // Keep hit testing tied to the control bounds, not to the moving glyphs.
        // Without this transparent surface, the UI mark can animate away from the
        // pointer, immediately firing MouseLeave and interrupting the reveal.
        drawingContext.DrawRectangle(
            Brushes.Transparent,
            null,
            new Rect(0, 0, ActualWidth, ActualHeight));

        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var culture = CultureInfo.CurrentUICulture;
        var typeface = new Typeface(FontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        var prefix = CreateText("Cheems", 25, typeface, culture, pixelsPerDip);
        var mark = CreateText("UI", 25, typeface, culture, pixelsPerDip);

        const double gap = 1.0;
        var fullWidth = prefix.WidthIncludingTrailingWhitespace + gap + mark.WidthIncludingTrailingWhitespace;
        var fullLeft = Math.Max(0, (ActualWidth - fullWidth) / 2.0);
        var collapsedMarkLeft = Math.Max(0, (ActualWidth - mark.WidthIncludingTrailingWhitespace) / 2.0);
        var expandedMarkLeft = fullLeft + prefix.WidthIncludingTrailingWhitespace + gap;
        var markLeft = Lerp(collapsedMarkLeft, expandedMarkLeft, _logoProgress);
        const double logoTop = 5.0;

        // The source suffix is a stroked SVG. Keep that visual language for "Cheems"
        // and reveal it from left to right as the logo expands.
        if (_logoProgress > 0.0001)
        {
            drawingContext.PushClip(new RectangleGeometry(
                new Rect(fullLeft - 2, 0, (prefix.WidthIncludingTrailingWhitespace + 4) * _logoProgress, 39)));
            var prefixGeometry = prefix.BuildGeometry(new Point(fullLeft, logoTop));
            drawingContext.DrawGeometry(null, GoldOutlinePen, prefixGeometry);
            drawingContext.Pop();
        }

        drawingContext.DrawText(mark, new Point(markLeft, logoTop));

        // The first Uiverse glyph has a short underline as part of the SVG mark.
        drawingContext.DrawRoundedRectangle(
            GoldBrush,
            null,
            new Rect(markLeft + 1, logoTop + 29, Math.Max(8, mark.WidthIncludingTrailingWhitespace - 2), 3),
            1.5,
            1.5);

        if (_captionProgress > 0.0001)
        {
            DrawSpacedCaption(drawingContext, "cheems.cn", typeface, culture, pixelsPerDip);
        }
    }

    private void DrawSpacedCaption(
        DrawingContext drawingContext,
        string text,
        Typeface typeface,
        CultureInfo culture,
        double pixelsPerDip)
    {
        const double fontSize = 7.0;
        var spacing = 5.2 * _captionProgress;
        var glyphs = new FormattedText[text.Length];
        var totalWidth = spacing * Math.Max(0, text.Length - 1);

        for (var index = 0; index < text.Length; index++)
        {
            glyphs[index] = CreateText(text[index].ToString(), fontSize, typeface, culture, pixelsPerDip);
            totalWidth += glyphs[index].WidthIncludingTrailingWhitespace;
        }

        var x = (ActualWidth - totalWidth) / 2.0;
        var opacityBrush = GoldBrush.Clone();
        opacityBrush.Opacity = _captionProgress;
        opacityBrush.Freeze();

        foreach (var glyph in glyphs)
        {
            glyph.SetForegroundBrush(opacityBrush);
            drawingContext.DrawText(glyph, new Point(x, 43));
            x += glyph.WidthIncludingTrailingWhitespace + spacing;
        }
    }

    private static FormattedText CreateText(
        string text,
        double fontSize,
        Typeface typeface,
        CultureInfo culture,
        double pixelsPerDip)
    {
        return new FormattedText(
            text,
            culture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            GoldBrush,
            pixelsPerDip);
    }

    private void BeginTransition(double target)
    {
        var now = Stopwatch.GetTimestamp();
        UpdateProgress(now);

        _logoStart = _logoProgress;
        _captionStart = _captionProgress;
        _target = target;
        _transitionStartedAt = now;

        if (!SystemParameters.ClientAreaAnimation)
        {
            _logoProgress = target;
            _captionProgress = target;
            StopRendering();
            InvalidateVisual();
            return;
        }

        StartRendering();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var complete = UpdateProgress(Stopwatch.GetTimestamp());
        InvalidateVisual();

        if (complete)
        {
            StopRendering();
        }
    }

    private bool UpdateProgress(long now)
    {
        if (_transitionStartedAt == 0)
        {
            return true;
        }

        var elapsedSeconds = (now - _transitionStartedAt) / (double)Stopwatch.Frequency;
        _logoProgress = Advance(_logoStart, _target, elapsedSeconds, 0, LogoDurationSeconds);
        _captionProgress = Advance(
            _captionStart,
            _target,
            elapsedSeconds,
            CaptionDelaySeconds,
            CaptionDurationSeconds);

        return elapsedSeconds >= CaptionDelaySeconds + CaptionDurationSeconds;
    }

    private static double Advance(double start, double target, double elapsed, double delay, double duration)
    {
        if (elapsed <= delay)
        {
            return start;
        }

        var linearProgress = Math.Clamp((elapsed - delay) / duration, 0, 1);
        return Lerp(start, target, EaseInOut(linearProgress));
    }

    // CSS ease-in-out: cubic-bezier(0.42, 0, 0.58, 1).
    private static double EaseInOut(double x)
    {
        var lower = 0.0;
        var upper = 1.0;
        var t = x;

        for (var index = 0; index < 10; index++)
        {
            t = (lower + upper) / 2.0;
            var sampleX = CubicBezier(t, 0.42, 0.58);
            if (sampleX < x)
            {
                lower = t;
            }
            else
            {
                upper = t;
            }
        }

        return CubicBezier(t, 0, 1);
    }

    private static double CubicBezier(double t, double control1, double control2)
    {
        var inverse = 1 - t;
        return 3 * inverse * inverse * t * control1
               + 3 * inverse * t * t * control2
               + t * t * t;
    }

    private static double Lerp(double start, double end, double progress) => start + ((end - start) * progress);

    private void StartRendering()
    {
        if (_isRendering)
        {
            return;
        }

        CompositionTarget.Rendering += OnRendering;
        _isRendering = true;
    }

    private void StopRendering()
    {
        if (!_isRendering)
        {
            return;
        }

        CompositionTarget.Rendering -= OnRendering;
        _isRendering = false;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => StopRendering();

    private static Brush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Pen CreateFrozenPen(Brush brush, double thickness)
    {
        var pen = new Pen(brush, thickness);
        pen.Freeze();
        return pen;
    }
}
