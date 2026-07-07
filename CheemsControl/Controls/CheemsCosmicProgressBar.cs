using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CheemsControl;

/// <summary>
/// Uiverse rust_1966 星空粒子进度条的 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartViewportName, Type = typeof(Canvas))]
[TemplatePart(Name = PartProgressGroupName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartProgressTextName, Type = typeof(TextBlock))]
public sealed class CheemsCosmicProgressBar : ProgressBar
{
    private const string PartViewportName = "PartViewport";
    private const string PartProgressGroupName = "PartProgressGroup";
    private const string PartProgressTextName = "PartProgressText";
    private static readonly string[] ParticleNames =
    {
        "PartParticleOne", "PartParticleTwo", "PartParticleThree", "PartParticleFour", "PartParticleFive"
    };

    private static readonly double[] ParticleLeft = { 0.20, 0.70, 0.50, 0.40, 0.60 };
    private static readonly double[] ParticleTop = { 0.10, 0.30, 0.50, 0.80, 0.90 };

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
        nameof(Step),
        typeof(double),
        typeof(CheemsCosmicProgressBar),
        new FrameworkPropertyMetadata(0.1),
        value => value is double step && double.IsFinite(step) && step > 0);

    private readonly FrameworkElement?[] _particles = new FrameworkElement?[5];
    private Canvas? _viewport;
    private FrameworkElement? _progressGroup;
    private TextBlock? _progressText;
    private RectangleGeometry? _viewportClip;
    private bool _dragArmed;
    private bool _isDragging;
    private double _dragStartX;
    private double _dragStartValue;

    static CheemsCosmicProgressBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsCosmicProgressBar),
            new FrameworkPropertyMetadata(typeof(CheemsCosmicProgressBar)));
    }

    /// <summary>
    /// 拖动时的数值步进，默认 0.1。
    /// </summary>
    public double Step
    {
        get => (double)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public override void OnApplyTemplate()
    {
        if (_viewport is not null)
        {
            _viewport.SizeChanged -= OnViewportSizeChanged;
        }

        base.OnApplyTemplate();
        _viewport = GetTemplateChild(PartViewportName) as Canvas;
        _progressGroup = GetTemplateChild(PartProgressGroupName) as FrameworkElement;
        _progressText = GetTemplateChild(PartProgressTextName) as TextBlock;
        for (var index = 0; index < ParticleNames.Length; index++)
        {
            _particles[index] = GetTemplateChild(ParticleNames[index]) as FrameworkElement;
        }

        if (_viewport is not null)
        {
            _viewportClip = new RectangleGeometry();
            _viewport.Clip = _viewportClip;
            _viewport.SizeChanged += OnViewportSizeChanged;
        }

        UpdateVisuals();
    }

    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        UpdateVisuals();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == MinimumProperty || e.Property == MaximumProperty)
        {
            UpdateVisuals();
        }
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        if (!IsEnabled || _viewport is null || _viewport.ActualWidth <= 0)
        {
            return;
        }

        var position = e.GetPosition(_viewport);
        if (position.X < 0 || position.X > _viewport.ActualWidth
            || position.Y < 0 || position.Y > _viewport.ActualHeight)
        {
            return;
        }

        _dragArmed = true;
        _isDragging = false;
        _dragStartX = position.X;
        _dragStartValue = Value;
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        if (!_dragArmed || _viewport is null)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            EndDrag();
            return;
        }

        var horizontalDelta = e.GetPosition(_viewport).X - _dragStartX;
        if (!_isDragging && Math.Abs(horizontalDelta) < SystemParameters.MinimumHorizontalDragDistance)
        {
            return;
        }

        _isDragging = true;
        var range = Maximum - Minimum;
        if (range > 0 && _viewport.ActualWidth > 0)
        {
            var rawValue = _dragStartValue + ((horizontalDelta / _viewport.ActualWidth) * range);
            SetCurrentValue(ValueProperty, SnapToStep(rawValue));
        }

        e.Handled = true;
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        if (!_dragArmed)
        {
            return;
        }

        EndDrag();
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        _dragArmed = false;
        _isDragging = false;
        base.OnLostMouseCapture(e);
    }

    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e) => UpdateVisuals();

    private void UpdateVisuals()
    {
        if (_viewport is null)
        {
            return;
        }

        var width = Math.Max(0, _viewport.ActualWidth);
        var height = Math.Max(0, _viewport.ActualHeight);
        if (_viewportClip is not null)
        {
            _viewportClip.Rect = new Rect(0, 0, width, height);
            _viewportClip.RadiusX = height / 2;
            _viewportClip.RadiusY = height / 2;
        }

        if (_progressGroup is not null)
        {
            _progressGroup.Width = width * CalculateProgress();
        }

        for (var index = 0; index < _particles.Length; index++)
        {
            if (_particles[index] is not FrameworkElement particle)
            {
                continue;
            }

            Canvas.SetLeft(particle, width * ParticleLeft[index]);
            Canvas.SetTop(particle, height * ParticleTop[index]);
        }

        if (_progressText is not null)
        {
            _progressText.Text = $"{CalculateProgress() * 100:0.#}%";
        }
    }

    private double CalculateProgress()
    {
        var range = Maximum - Minimum;
        return range <= 0 ? 0 : Math.Clamp((Value - Minimum) / range, 0, 1);
    }

    private double SnapToStep(double value)
    {
        var steps = Math.Round((value - Minimum) / Step, MidpointRounding.AwayFromZero);
        return Math.Clamp(Minimum + (steps * Step), Minimum, Maximum);
    }

    private void EndDrag()
    {
        _dragArmed = false;
        _isDragging = false;
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }
    }
}
