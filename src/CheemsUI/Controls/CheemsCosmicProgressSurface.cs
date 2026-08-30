using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>
/// 将星空进度条的轨道和进度合成为一个画刷，并只栅格化一次外层圆角。
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CheemsCosmicProgressSurface : FrameworkElement
{
    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(Brush), typeof(CheemsCosmicProgressSurface),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ProgressBrushProperty = DependencyProperty.Register(
        nameof(ProgressBrush), typeof(Brush), typeof(CheemsCosmicProgressSurface),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(CheemsCosmicProgressSurface),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender),
        value => value is double progress && double.IsFinite(progress));

    public Brush? TrackBrush
    {
        get => (Brush?)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public Brush? ProgressBrush
    {
        get => (Brush?)GetValue(ProgressBrushProperty);
        set => SetValue(ProgressBrushProperty, value);
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var width = Math.Max(0, ActualWidth);
        var height = Math.Max(0, ActualHeight);
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var bounds = new Rect(0, 0, width, height);
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(
            TrackBrush ?? Brushes.Transparent,
            null,
            new RectangleGeometry(bounds)));

        var progressWidth = width * Math.Clamp(Progress, 0, 1);
        if (progressWidth > 0)
        {
            // 进度颜色先在矩形画刷中完成合成，最外层圆角只在下面绘制一次。
            drawing.Children.Add(new GeometryDrawing(
                ProgressBrush ?? Brushes.Transparent,
                null,
                new RectangleGeometry(new Rect(0, 0, progressWidth, height))));
        }

        var compositeBrush = new DrawingBrush(drawing) { Stretch = Stretch.Fill };
        var radius = height / 2;
        drawingContext.DrawRoundedRectangle(compositeBrush, null, bounds, radius, radius);
    }
}
