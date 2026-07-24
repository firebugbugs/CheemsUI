using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace CheemsUI;

/// <summary>
/// Uiverse AshtonLiou 分段 3D 沟槽进度条的 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartModelHostName, Type = typeof(ModelVisual3D))]
[TemplatePart(Name = PartTextHostName, Type = typeof(Viewport2DVisual3D))]
public sealed class CheemsCircuitProgressBar : CheemsDraggableProgressBar
{
    private const string PartModelHostName = "PartModelHost";
    private const string PartTextHostName = "PartTextHost";
    private const double SegmentHeight = 36.4;
    private const double BorderSize = 3.2;
    private const double TotalProgressUnits = 725;
    private const double TransitionDurationSeconds = 0.15;

    private static readonly double[] RotationXByRow = { -20, -10, -5, 5, 10, 20 };
    private static readonly double[] RotationYByColumn = { 20, 10, 5, -5, -10, -20 };

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(CheemsCircuitProgressBar),
        new FrameworkPropertyMetadata("LOADING..."));

    public static readonly DependencyProperty ProgressBrushProperty = DependencyProperty.Register(
        nameof(ProgressBrush),
        typeof(Brush),
        typeof(CheemsCircuitProgressBar),
        new FrameworkPropertyMetadata(Brushes.SeaGreen, OnAppearancePropertyChanged));

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush),
        typeof(Brush),
        typeof(CheemsCircuitProgressBar),
        new FrameworkPropertyMetadata(Brushes.White, OnAppearancePropertyChanged));

    private readonly List<SegmentVisual> _segments = new();
    private ModelVisual3D? _modelHost;
    private Viewport2DVisual3D? _textHost;
    private AxisAngleRotation3D? _rotationX;
    private AxisAngleRotation3D? _rotationY;
    private double _currentRotationX;
    private double _currentRotationY;
    private double _startRotationX;
    private double _startRotationY;
    private double _targetRotationX;
    private double _targetRotationY;
    private long _transitionStartedAt;
    private bool _renderingSubscribed;

    static CheemsCircuitProgressBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsCircuitProgressBar),
            new FrameworkPropertyMetadata(typeof(CheemsCircuitProgressBar)));
    }

    public CheemsCircuitProgressBar()
    {
        Unloaded += OnUnloaded;
    }

    /// <summary>
    /// 进度条上方的可选文字。默认 LOADING...；设为空字符串可隐藏。
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Brush ProgressBrush
    {
        get => (Brush)GetValue(ProgressBrushProperty);
        set => SetValue(ProgressBrushProperty, value);
    }

    public Brush TrackBrush
    {
        get => (Brush)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _modelHost = GetTemplateChild(PartModelHostName) as ModelVisual3D;
        _textHost = GetTemplateChild(PartTextHostName) as Viewport2DVisual3D;
        _segments.Clear();

        if (_modelHost is not null)
        {
            BuildModel();
            ApplyRotation();
            UpdateProgressVisuals();
        }
    }

    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        UpdateProgressVisuals();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == MinimumProperty || e.Property == MaximumProperty)
        {
            UpdateProgressVisuals();
        }
    }

    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var position = e.GetPosition(this);
        var column = Math.Clamp((int)(position.X / ActualWidth * 6), 0, 5);
        var row = Math.Clamp((int)(position.Y / ActualHeight * 6), 0, 5);
        BeginRotationTransition(RotationXByRow[row], RotationYByColumn[column]);
    }

    protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        BeginRotationTransition(0, 0);
    }

    private void BuildModel()
    {
        if (_modelHost is null)
        {
            return;
        }

        var group = new Model3DGroup();
        group.Children.Add(new AmbientLight(Colors.White));
        var progressOffset = 0d;

        AddSegment(group, ref progressOffset, 0, 0, 100, 0, 100, roundStart: true);
        AddSegment(group, ref progressOffset, 100, 0, 100, -50, 50);
        AddSegment(group, ref progressOffset, 100, -50, 130, -50, 30);
        AddSegment(group, ref progressOffset, 130, -50, 130, 0, 50);
        AddSegment(group, ref progressOffset, 130, 0, 140, 0, 10);
        AddSegment(group, ref progressOffset, 140, 0, 140, -100, 100);
        AddSegment(group, ref progressOffset, 140, -100, 155, -100, 15);
        AddSegment(group, ref progressOffset, 155, -100, 155, 40, 140);
        AddSegment(group, ref progressOffset, 155, 40, 205, 40, 60);
        AddSegment(group, ref progressOffset, 205, 40, 205, -20, 60);
        AddSegment(group, ref progressOffset, 205, -20, 235, -20, 50);
        AddSegment(group, ref progressOffset, 235, -20, 235, 0, 20);
        AddSegment(group, ref progressOffset, 235, 0, 275, 0, 40, roundEnd: true);

        _modelHost.Content = group;

        _rotationX = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0);
        _rotationY = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
        var transform = new Transform3DGroup();
        transform.Children.Add(new RotateTransform3D(_rotationX, new Point3D(137.5, 0, 0)));
        transform.Children.Add(new RotateTransform3D(_rotationY, new Point3D(137.5, 0, 0)));
        _modelHost.Transform = transform;
        if (_textHost is not null)
        {
            _textHost.Transform = transform;
        }
    }

    private void AddSegment(
        Model3DGroup group,
        ref double progressOffset,
        double startX,
        double startZ,
        double endX,
        double endZ,
        double progressUnits,
        bool roundStart = false,
        bool roundEnd = false)
    {
        var model = new GeometryModel3D
        {
            Geometry = CreateSegmentMesh(startX, startZ, endX, endZ, roundStart, roundEnd)
        };
        var visualLength = Math.Sqrt(
            ((endX - startX) * (endX - startX))
            + ((endZ - startZ) * (endZ - startZ)));
        var segment = new SegmentVisual(
            model,
            progressOffset,
            progressUnits,
            visualLength,
            roundStart,
            roundEnd);
        _segments.Add(segment);
        progressOffset += progressUnits;
        SetSegmentMaterial(segment, 0);
        group.Children.Add(model);
    }

    private MeshGeometry3D CreateSegmentMesh(
        double startX,
        double startZ,
        double endX,
        double endZ,
        bool roundStart,
        bool roundEnd)
    {
        var deltaX = endX - startX;
        var deltaZ = endZ - startZ;
        var length = Math.Sqrt((deltaX * deltaX) + (deltaZ * deltaZ));
        var directionX = deltaX / length;
        var directionZ = deltaZ / length;
        var halfHeight = SegmentHeight / 2;
        var outline = CreateRoundedOutline(length, halfHeight, roundStart ? 10 : 0, roundEnd ? 10 : 0);
        var mesh = new MeshGeometry3D();

        mesh.Positions.Add(new Point3D(
            startX + (directionX * length / 2),
            0,
            startZ + (directionZ * length / 2)));
        mesh.TextureCoordinates.Add(new Point(0.5, 0.5));

        foreach (var point in outline)
        {
            mesh.Positions.Add(new Point3D(
                startX + (directionX * point.X),
                point.Y,
                startZ + (directionZ * point.X)));
            mesh.TextureCoordinates.Add(new Point(point.X / length, (halfHeight - point.Y) / SegmentHeight));
        }

        for (var index = 0; index < outline.Count; index++)
        {
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(index + 1);
            mesh.TriangleIndices.Add(((index + 1) % outline.Count) + 1);
        }

        mesh.Freeze();
        return mesh;
    }

    private static List<Point> CreateRoundedOutline(
        double width,
        double halfHeight,
        double startRadius,
        double endRadius)
    {
        var points = new List<Point>();
        AppendCorner(points, startRadius, halfHeight - startRadius, startRadius, 180, 90);
        points.Add(new Point(width - endRadius, halfHeight));
        AppendCorner(points, width - endRadius, halfHeight - endRadius, endRadius, 90, 0);
        points.Add(new Point(width, -halfHeight + endRadius));
        AppendCorner(points, width - endRadius, -halfHeight + endRadius, endRadius, 0, -90);
        points.Add(new Point(startRadius, -halfHeight));
        AppendCorner(points, startRadius, -halfHeight + startRadius, startRadius, -90, -180);
        return points;
    }

    private static void AppendCorner(
        ICollection<Point> points,
        double centerX,
        double centerY,
        double radius,
        double startAngle,
        double endAngle)
    {
        if (radius <= 0)
        {
            points.Add(new Point(centerX, centerY));
            return;
        }

        const int steps = 5;
        for (var step = 0; step <= steps; step++)
        {
            var angle = (startAngle + ((endAngle - startAngle) * step / steps)) * Math.PI / 180;
            points.Add(new Point(
                centerX + (Math.Cos(angle) * radius),
                centerY + (Math.Sin(angle) * radius)));
        }
    }

    private void UpdateProgressVisuals()
    {
        if (_segments.Count == 0)
        {
            return;
        }

        var range = Maximum - Minimum;
        var progress = range <= 0 ? 0 : Math.Clamp((Value - Minimum) / range, 0, 1);
        var filledUnits = progress * TotalProgressUnits;

        foreach (var segment in _segments)
        {
            var fraction = Math.Clamp(
                (filledUnits - segment.ProgressOffset) / segment.ProgressUnits,
                0,
                1);
            if (Math.Abs(fraction - segment.LastFraction) < 0.0001)
            {
                continue;
            }

            SetSegmentMaterial(segment, fraction);
        }
    }

    private void SetSegmentMaterial(SegmentVisual segment, double fraction)
    {
        var borderRatio = BorderSize / SegmentHeight;
        var horizontalBorderRatio = BorderSize / segment.VisualLength;
        var innerTop = borderRatio * 2;
        var innerHeight = 1 - (borderRatio * 4);
        var fillStart = segment.HasStartCap ? horizontalBorderRatio * 2 : 0;
        var fillEnd = segment.HasEndCap ? 1 - (horizontalBorderRatio * 2) : 1;
        var fillWidth = Math.Max(0, (fillEnd - fillStart) * fraction);
        var drawing = new DrawingGroup();

        // 先画完整绿色外形，再以等距内缩的白色几何覆盖，得到四角等厚的 CSS border。
        drawing.Children.Add(new GeometryDrawing(
            CloneBrush(ProgressBrush),
            null,
            new RectangleGeometry(new Rect(0, 0, 1, 1))));
        drawing.Children.Add(new GeometryDrawing(
            CloneBrush(TrackBrush),
            null,
            CreateFillGeometry(
                new Rect(
                    segment.HasStartCap ? horizontalBorderRatio : 0,
                    borderRatio,
                    1 - ((segment.HasStartCap ? horizontalBorderRatio : 0)
                         + (segment.HasEndCap ? horizontalBorderRatio : 0)),
                    1 - (2 * borderRatio)),
                segment.HasStartCap,
                segment.HasEndCap,
                (10 - BorderSize) / segment.VisualLength,
                (10 - BorderSize) / SegmentHeight)));

        // ::before 位于边框内侧 0.2rem，因此上下及封口处都保留白色间隙。
        if (fillWidth > 0)
        {
            drawing.Children.Add(new GeometryDrawing(
                CloneBrush(ProgressBrush),
                null,
                CreateFillGeometry(
                    new Rect(fillStart, innerTop, fillWidth, innerHeight),
                    segment.HasStartCap,
                    segment.HasEndCap,
                    5 / segment.VisualLength,
                    5 / SegmentHeight)));
        }

        drawing.Freeze();
        var brush = new DrawingBrush(drawing)
        {
            Stretch = Stretch.Fill,
            Viewbox = new Rect(0, 0, 1, 1),
            ViewboxUnits = BrushMappingMode.Absolute
        };
        brush.Freeze();
        var material = new DiffuseMaterial(brush) { AmbientColor = Colors.White };
        material.Freeze();
        segment.Model.Material = material;
        segment.Model.BackMaterial = material;
        segment.LastFraction = fraction;
    }

    private static Geometry CreateFillGeometry(
        Rect rect,
        bool roundStart,
        bool roundEnd,
        double radiusX,
        double radiusY)
    {
        radiusX = Math.Min(radiusX, rect.Width / 2);
        radiusY = Math.Min(radiusY, rect.Height / 2);
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(
                new Point(rect.Left + (roundStart ? radiusX : 0), rect.Top),
                isFilled: true,
                isClosed: true);
            context.LineTo(new Point(rect.Right - (roundEnd ? radiusX : 0), rect.Top), true, false);
            if (roundEnd)
            {
                context.ArcTo(
                    new Point(rect.Right, rect.Top + radiusY),
                    new Size(radiusX, radiusY),
                    0,
                    false,
                    SweepDirection.Clockwise,
                    true,
                    false);
            }

            context.LineTo(new Point(rect.Right, rect.Bottom - (roundEnd ? radiusY : 0)), true, false);
            if (roundEnd)
            {
                context.ArcTo(
                    new Point(rect.Right - radiusX, rect.Bottom),
                    new Size(radiusX, radiusY),
                    0,
                    false,
                    SweepDirection.Clockwise,
                    true,
                    false);
            }

            context.LineTo(new Point(rect.Left + (roundStart ? radiusX : 0), rect.Bottom), true, false);
            if (roundStart)
            {
                context.ArcTo(
                    new Point(rect.Left, rect.Bottom - radiusY),
                    new Size(radiusX, radiusY),
                    0,
                    false,
                    SweepDirection.Clockwise,
                    true,
                    false);
            }

            context.LineTo(new Point(rect.Left, rect.Top + (roundStart ? radiusY : 0)), true, false);
            if (roundStart)
            {
                context.ArcTo(
                    new Point(rect.Left + radiusX, rect.Top),
                    new Size(radiusX, radiusY),
                    0,
                    false,
                    SweepDirection.Clockwise,
                    true,
                    false);
            }
        }

        geometry.Freeze();
        return geometry;
    }

    private void BeginRotationTransition(double targetX, double targetY)
    {
        var now = Stopwatch.GetTimestamp();
        UpdateRotation(now);
        if (Math.Abs(targetX - _targetRotationX) < 0.001
            && Math.Abs(targetY - _targetRotationY) < 0.001)
        {
            return;
        }

        _startRotationX = _currentRotationX;
        _startRotationY = _currentRotationY;
        _targetRotationX = targetX;
        _targetRotationY = targetY;
        _transitionStartedAt = now;

        if (!SystemParameters.ClientAreaAnimation)
        {
            _currentRotationX = targetX;
            _currentRotationY = targetY;
            ApplyRotation();
            UnsubscribeRendering();
            return;
        }

        SubscribeRendering();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (UpdateRotation(Stopwatch.GetTimestamp()))
        {
            UnsubscribeRendering();
        }

        ApplyRotation();
    }

    private bool UpdateRotation(long now)
    {
        if (_transitionStartedAt == 0)
        {
            return true;
        }

        var elapsed = (now - _transitionStartedAt) / (double)Stopwatch.Frequency;
        var progress = Math.Clamp(elapsed / TransitionDurationSeconds, 0, 1);
        _currentRotationX = Lerp(_startRotationX, _targetRotationX, progress);
        _currentRotationY = Lerp(_startRotationY, _targetRotationY, progress);
        return progress >= 1;
    }

    private void ApplyRotation()
    {
        if (_rotationX is not null)
        {
            _rotationX.Angle = _currentRotationX;
        }

        if (_rotationY is not null)
        {
            _rotationY.Angle = _currentRotationY;
        }
    }

    private static void OnAppearancePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var control = (CheemsCircuitProgressBar)dependencyObject;
        if (control._modelHost is null)
        {
            return;
        }

        control._segments.Clear();
        control.BuildModel();
        control.ApplyRotation();
        control.UpdateProgressVisuals();
    }

    private static Brush CloneBrush(Brush brush)
    {
        var clone = brush.CloneCurrentValue();
        if (clone.CanFreeze)
        {
            clone.Freeze();
        }

        return clone;
    }

    private void SubscribeRendering()
    {
        if (_renderingSubscribed)
        {
            return;
        }

        CompositionTarget.Rendering += OnRendering;
        _renderingSubscribed = true;
    }

    private void UnsubscribeRendering()
    {
        if (!_renderingSubscribed)
        {
            return;
        }

        CompositionTarget.Rendering -= OnRendering;
        _renderingSubscribed = false;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => UnsubscribeRendering();

    private static double Lerp(double start, double end, double progress) =>
        start + ((end - start) * progress);

    private sealed class SegmentVisual
    {
        public SegmentVisual(
            GeometryModel3D model,
            double progressOffset,
            double progressUnits,
            double visualLength,
            bool hasStartCap,
            bool hasEndCap)
        {
            Model = model;
            ProgressOffset = progressOffset;
            ProgressUnits = progressUnits;
            VisualLength = visualLength;
            HasStartCap = hasStartCap;
            HasEndCap = hasEndCap;
        }

        public GeometryModel3D Model { get; }
        public double ProgressOffset { get; }
        public double ProgressUnits { get; }
        public double VisualLength { get; }
        public bool HasStartCap { get; }
        public bool HasEndCap { get; }
        public double LastFraction { get; set; } = double.NaN;
    }
}
