using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CheemsControl;

/// <summary>Uiverse adamgiebl layered 3D button 的 WPF 等价实现。</summary>
public sealed class CheemsLayered3DButton : Button
{
    private static readonly Point[] Rest = { new(0, 0), new(0, 0), new(0, 0), new(0, 0), new(0, 0) };
    private static readonly Point[] Hover = { new(0, 0), new(10, -10), new(20, -20), new(30, -30), new(40, -40) };
    private static readonly Point[] Pressed = { new(0, 0), new(5, -5), new(10, -10), new(15, -15), new(20, -20) };
    private static readonly double[] HoverOpacity = { .2, .4, .6, .8, 1 };
    private readonly TranslateTransform?[] _moves = new TranslateTransform?[5];
    private readonly SolidColorBrush?[] _fills = new SolidColorBrush?[5];
    private readonly Point[] _from = new Point[5];
    private readonly Point[] _to = new Point[5];
    private readonly double[] _fromOpacity = new double[5];
    private readonly double[] _toOpacity = new double[5];
    private readonly Color[] _fromColor = new Color[5];
    private Color _toColor;
    private long _startedAt;
    private double _duration;
    private bool _subscribed;

    static CheemsLayered3DButton() => DefaultStyleKeyProperty.OverrideMetadata(
        typeof(CheemsLayered3DButton), new FrameworkPropertyMetadata(typeof(CheemsLayered3DButton)));

    public CheemsLayered3DButton() => Unloaded += (_, _) => Unsubscribe();

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        for (var i = 0; i < 5; i++)
        {
            if (GetTemplateChild($"PartLayer{i + 1}") is not Border layer) continue;
            _moves[i] = new TranslateTransform();
            _fills[i] = new SolidColorBrush(Color.FromRgb(42, 42, 42));
            layer.RenderTransform = _moves[i];
            layer.Background = _fills[i];
        }
    }

    protected override void OnMouseEnter(MouseEventArgs e) { base.OnMouseEnter(e); Begin(Hover, HoverOpacity, Color.FromRgb(82, 225, 159), .3); }
    protected override void OnMouseLeave(MouseEventArgs e) { base.OnMouseLeave(e); Begin(Rest, Ones(), Color.FromRgb(42, 42, 42), 1.1); }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) { base.OnMouseLeftButtonDown(e); Begin(Pressed, HoverOpacity, Color.FromRgb(82, 225, 159), .3); }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        Begin(IsMouseOver ? Hover : Rest, IsMouseOver ? HoverOpacity : Ones(),
            IsMouseOver ? Color.FromRgb(82, 225, 159) : Color.FromRgb(42, 42, 42), IsMouseOver ? .3 : 1.1);
    }

    private static double[] Ones() => new[] { 1d, 1d, 1d, 1d, 1d };

    private void Begin(Point[] target, double[] opacity, Color color, double duration)
    {
        for (var i = 0; i < 5; i++)
        {
            _from[i] = new Point(_moves[i]?.X ?? 0, _moves[i]?.Y ?? 0);
            _to[i] = target[i];
            _fromOpacity[i] = (GetTemplateChild($"PartLayer{i + 1}") as FrameworkElement)?.Opacity ?? 1;
            _toOpacity[i] = opacity[i];
            _fromColor[i] = _fills[i]?.Color ?? Color.FromRgb(42, 42, 42);
        }
        _toColor = color; _duration = duration; _startedAt = Stopwatch.GetTimestamp();
        if (_subscribed) return;
        CompositionTarget.Rendering += Render; _subscribed = true;
    }

    private void Render(object? sender, EventArgs e)
    {
        var p = Math.Clamp((Stopwatch.GetTimestamp() - _startedAt) / (double)Stopwatch.Frequency / _duration, 0, 1);
        p = 1 - Math.Pow(1 - p, 3); // ease-out
        for (var i = 0; i < 5; i++)
        {
            if (_moves[i] is { } move) { move.X = Lerp(_from[i].X, _to[i].X, p); move.Y = Lerp(_from[i].Y, _to[i].Y, p); }
            if (GetTemplateChild($"PartLayer{i + 1}") is FrameworkElement layer) layer.Opacity = Lerp(_fromOpacity[i], _toOpacity[i], p);
            if (_fills[i] is { } fill) fill.Color = Mix(_fromColor[i], _toColor, p);
        }
        if (p >= 1) Unsubscribe();
    }

    private void Unsubscribe() { if (!_subscribed) return; CompositionTarget.Rendering -= Render; _subscribed = false; }
    private static double Lerp(double a, double b, double p) => a + (b - a) * p;
    private static Color Mix(Color a, Color b, double p) => Color.FromArgb((byte)Lerp(a.A,b.A,p),(byte)Lerp(a.R,b.R,p),(byte)Lerp(a.G,b.G,p),(byte)Lerp(a.B,b.B,p));
}
