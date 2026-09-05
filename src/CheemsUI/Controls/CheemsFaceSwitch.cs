using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>Uiverse Shoh2008 face switch with a sliding thumb and flipping expression.</summary>
/// <remarks>
/// The source's <c>--size</c> is 40 px: the track is 88×40, the thumb is 32×32,
/// and all checked-state transitions use 0.35 seconds ease-in-out.
/// </remarks>
[TemplatePart(Name = "PartTrack", Type = typeof(Border))]
[TemplatePart(Name = "PartThumb", Type = typeof(Border))]
[TemplatePart(Name = "PartEyes", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PartMouth", Type = typeof(FrameworkElement))]
public sealed class CheemsFaceSwitch : ToggleButton
{
    static CheemsFaceSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsFaceSwitch),
            new FrameworkPropertyMetadata(typeof(CheemsFaceSwitch)));
    }
}

/// <summary>Continuously morphs the clipped bullet pseudo-element into its filled smile.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CheemsFaceMouth : FrameworkElement
{
    public static readonly DependencyProperty SmileProgressProperty = DependencyProperty.Register(
        nameof(SmileProgress),
        typeof(double),
        typeof(CheemsFaceMouth),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, null, CoerceProgress));

    // Keep the original dependency-property name so existing BAML and Hot Reload
    // sessions remain binary compatible. The brush now fills the clipped glyph.
    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke),
        typeof(Brush),
        typeof(CheemsFaceMouth),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public double SmileProgress
    {
        get => (double)GetValue(SmileProgressProperty);
        set => SetValue(SmileProgressProperty, value);
    }

    public Brush? Stroke
    {
        get => (Brush?)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (Stroke is null)
        {
            return;
        }

        // CSS uses the "●" glyph inside a short overflow:hidden box.  In the checked
        // state that clipping produces a flat top with a rounded, filled lower half.
        // Draw that silhouette directly so it is independent of installed font metrics.
        var progress = SmileProgress;
        // Times New Roman's checked "●" occupies roughly 10 px of the 14 px clip.
        var halfWidth = 2.5 + (2.5 * progress);
        var top = 2.0 - progress;
        var depth = 2.0 + (4.5 * progress);
        var shoulder = 0.5 + (1.5 * progress);
        const double centerX = 7.0;
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(centerX - halfWidth, top), true, true);
            context.LineTo(new Point(centerX + halfWidth, top), true, false);
            context.LineTo(new Point(centerX + halfWidth, top + shoulder), true, false);
            context.BezierTo(
                new Point(centerX + halfWidth, top + shoulder + ((depth - shoulder) * 0.55)),
                new Point(centerX + (halfWidth * 0.55), top + depth),
                new Point(centerX, top + depth),
                true,
                false);
            context.BezierTo(
                new Point(centerX - (halfWidth * 0.55), top + depth),
                new Point(centerX - halfWidth, top + shoulder + ((depth - shoulder) * 0.55)),
                new Point(centerX - halfWidth, top + shoulder),
                true,
                false);
        }
        geometry.Freeze();
        drawingContext.DrawGeometry(Stroke, null, geometry);
    }

    private static object CoerceProgress(DependencyObject sender, object value) =>
        Math.Clamp((double)value, 0.0, 1.0);
}
