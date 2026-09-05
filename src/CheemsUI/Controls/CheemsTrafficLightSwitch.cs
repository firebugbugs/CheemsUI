using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>The three fixed signals exposed by <see cref="CheemsTrafficLightSwitch"/>.</summary>
public enum CheemsTrafficSignal
{
    Red,
    Yellow,
    Green
}

/// <summary>Uiverse Praashoo7 three-position illuminated traffic-light selector.</summary>
[TemplatePart(Name = PartRootName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartRedLightName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartYellowLightName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartGreenLightName, Type = typeof(FrameworkElement))]
public class CheemsTrafficLightSwitch : Control
{
    private const string PartRootName = "PartRoot";
    private const string PartRedLightName = "PartRedLight";
    private const string PartYellowLightName = "PartYellowLight";
    private const string PartGreenLightName = "PartGreenLight";
    private const double FlickerDelaySeconds = 0.3;
    private const double FlickerDurationSeconds = 0.2;
    private const double LightOffDurationSeconds = 1.0;

    public static readonly DependencyProperty SelectedSignalProperty = DependencyProperty.Register(
        nameof(SelectedSignal),
        typeof(CheemsTrafficSignal),
        typeof(CheemsTrafficLightSwitch),
        new FrameworkPropertyMetadata(
            CheemsTrafficSignal.Yellow,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnSelectedSignalChanged),
        value => Enum.IsDefined(typeof(CheemsTrafficSignal), value));

    public static readonly RoutedEvent SelectedSignalChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(SelectedSignalChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<CheemsTrafficSignal>),
        typeof(CheemsTrafficLightSwitch));

    private readonly LampState[] _lamps = { new(), new(), new() };
    private FrameworkElement? _root;
    private bool _renderingSubscribed;

    static CheemsTrafficLightSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsTrafficLightSwitch),
            new FrameworkPropertyMetadata(typeof(CheemsTrafficLightSwitch)));
    }

    public CheemsTrafficLightSwitch()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Gets or selects one of the three fixed traffic signals.</summary>
    public CheemsTrafficSignal SelectedSignal
    {
        get => (CheemsTrafficSignal)GetValue(SelectedSignalProperty);
        set => SetValue(SelectedSignalProperty, value);
    }

    public event RoutedPropertyChangedEventHandler<CheemsTrafficSignal> SelectedSignalChanged
    {
        add => AddHandler(SelectedSignalChangedEvent, value);
        remove => RemoveHandler(SelectedSignalChangedEvent, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild(PartRootName) as FrameworkElement;
        _lamps[0].Element = GetTemplateChild(PartRedLightName) as FrameworkElement;
        _lamps[1].Element = GetTemplateChild(PartYellowLightName) as FrameworkElement;
        _lamps[2].Element = GetTemplateChild(PartGreenLightName) as FrameworkElement;

        if (IsLoaded)
        {
            RestartAllAnimations();
        }
        else
        {
            ApplyImmediateState();
        }
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (IsEnabled && _root is not null)
        {
            var point = e.GetPosition(_root);
            const double firstCenterX = 52.32;
            const double centerY = 52.8;
            for (var index = 0; index < 3; index++)
            {
                var dx = point.X - (firstCenterX + (index * 80));
                var dy = point.Y - centerY;
                if ((dx * dx) + (dy * dy) <= 35 * 35)
                {
                    SelectedSignal = (CheemsTrafficSignal)index;
                    Focus();
                    e.Handled = true;
                    break;
                }
            }
        }

        base.OnPreviewMouseLeftButtonDown(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var index = (int)SelectedSignal;
        switch (e.Key)
        {
            case Key.Left:
            case Key.Up:
                SelectedSignal = (CheemsTrafficSignal)((index + 2) % 3);
                e.Handled = true;
                break;
            case Key.Right:
            case Key.Down:
            case Key.Space:
                SelectedSignal = (CheemsTrafficSignal)((index + 1) % 3);
                e.Handled = true;
                break;
            case Key.Home:
            case Key.D1:
            case Key.NumPad1:
                SelectedSignal = CheemsTrafficSignal.Red;
                e.Handled = true;
                break;
            case Key.D2:
            case Key.NumPad2:
                SelectedSignal = CheemsTrafficSignal.Yellow;
                e.Handled = true;
                break;
            case Key.End:
            case Key.D3:
            case Key.NumPad3:
                SelectedSignal = CheemsTrafficSignal.Green;
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }

    private static void OnSelectedSignalChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var control = (CheemsTrafficLightSwitch)sender;
        var oldValue = (CheemsTrafficSignal)e.OldValue;
        var newValue = (CheemsTrafficSignal)e.NewValue;
        control.BeginSignalTransition(oldValue, newValue);
        control.RaiseEvent(new RoutedPropertyChangedEventArgs<CheemsTrafficSignal>(
            oldValue,
            newValue,
            SelectedSignalChangedEvent));
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => RestartAllAnimations();

    private void OnUnloaded(object sender, RoutedEventArgs e) => UnsubscribeRendering();

    private void RestartAllAnimations()
    {
        var now = Stopwatch.GetTimestamp();
        for (var index = 0; index < _lamps.Length; index++)
        {
            _lamps[index].IsActive = index == (int)SelectedSignal;
            _lamps[index].StartedAt = now;
        }

        ApplyFrame(now);
        if (SystemParameters.ClientAreaAnimation)
        {
            SubscribeRendering();
        }
    }

    private void BeginSignalTransition(CheemsTrafficSignal oldValue, CheemsTrafficSignal newValue)
    {
        if (!IsLoaded || !SystemParameters.ClientAreaAnimation)
        {
            ApplyImmediateState();
            return;
        }

        var now = Stopwatch.GetTimestamp();
        _lamps[(int)oldValue].IsActive = false;
        _lamps[(int)oldValue].StartedAt = now;
        _lamps[(int)newValue].IsActive = true;
        _lamps[(int)newValue].StartedAt = now;
        ApplyFrame(now);
        SubscribeRendering();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (IsVisible)
        {
            ApplyFrame(Stopwatch.GetTimestamp());
        }
    }

    private void ApplyFrame(long now)
    {
        foreach (var lamp in _lamps)
        {
            if (lamp.Element is null)
            {
                continue;
            }

            var elapsed = (now - lamp.StartedAt) / (double)Stopwatch.Frequency;
            lamp.Element.Opacity = lamp.IsActive
                ? EvaluateFlicker(elapsed)
                : EvaluateLightOff(elapsed);
        }
    }

    private void ApplyImmediateState()
    {
        for (var index = 0; index < _lamps.Length; index++)
        {
            if (_lamps[index].Element is not null)
            {
                _lamps[index].Element!.Opacity = index == (int)SelectedSignal ? 1 : 0;
            }
        }
    }

    private static double EvaluateFlicker(double elapsed)
    {
        if (elapsed < FlickerDelaySeconds)
        {
            return 0;
        }

        var phase = ((elapsed - FlickerDelaySeconds) % FlickerDurationSeconds) / FlickerDurationSeconds;
        return phase <= 0.8
            ? Lerp(1, 0.8, CssEase(phase / 0.8))
            : Lerp(0.8, 1, CssEase((phase - 0.8) / 0.2));
    }

    private static double EvaluateLightOff(double elapsed)
    {
        if (elapsed >= LightOffDurationSeconds * 0.8)
        {
            return 0;
        }

        return Lerp(1, 0, CssEase(elapsed / (LightOffDurationSeconds * 0.8)));
    }

    /// <summary>CSS default easing: cubic-bezier(.25, .1, .25, 1).</summary>
    private static double CssEase(double progress)
    {
        progress = Math.Clamp(progress, 0, 1);
        var parameter = progress;

        // Invert the Bezier x component with Newton-Raphson, then evaluate y.
        for (var iteration = 0; iteration < 6; iteration++)
        {
            var x = CubicBezier(parameter, 0.25, 0.25);
            var derivative = CubicBezierDerivative(parameter, 0.25, 0.25);
            if (Math.Abs(derivative) < 0.000001)
            {
                break;
            }

            parameter = Math.Clamp(parameter - ((x - progress) / derivative), 0, 1);
        }

        return CubicBezier(parameter, 0.1, 1.0);
    }

    private static double CubicBezier(double parameter, double firstControl, double secondControl)
    {
        var inverse = 1 - parameter;
        return (3 * inverse * inverse * parameter * firstControl) +
               (3 * inverse * parameter * parameter * secondControl) +
               (parameter * parameter * parameter);
    }

    private static double CubicBezierDerivative(double parameter, double firstControl, double secondControl)
    {
        var inverse = 1 - parameter;
        return (3 * inverse * inverse * firstControl) +
               (6 * inverse * parameter * (secondControl - firstControl)) +
               (3 * parameter * parameter * (1 - secondControl));
    }

    private static double Lerp(double from, double to, double progress) => from + ((to - from) * progress);

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

    private sealed class LampState
    {
        public FrameworkElement? Element { get; set; }
        public bool IsActive { get; set; }
        public long StartedAt { get; set; }
    }
}
