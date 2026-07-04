using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CheemsControl;

/// <summary>
/// Uiverse augustin_4687 pixel hand button 的 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartDotsName, Type = typeof(Rectangle))]
[TemplatePart(Name = PartRearName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartRearShadowName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartRearHighlightName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartRearShadeName, Type = typeof(FrameworkElement))]
public sealed class CheemsPixelHandButton : Button
{
    private const string PartDotsName = "PartDots";
    private const string PartRearName = "PartRear";
    private const string PartRearShadowName = "PartRearShadow";
    private const string PartRearHighlightName = "PartRearHighlight";
    private const string PartRearShadeName = "PartRearShade";
    private const double DotAnimationDurationSeconds = 0.5;
    private const double DotTileSize = 8;
    private const double CoinBurstDurationSeconds = 0.2;
    private const double CoinBurstDistance = -96;

    private TranslateTransform? _dotTranslation;
    private readonly FrameworkElement?[] _coinLayers = new FrameworkElement?[4];
    private readonly TranslateTransform?[] _coinTranslations = new TranslateTransform?[4];
    private long _animationStartedAt;
    private long _coinBurstStartedAt;
    private bool _coinBurstActive;
    private bool _renderingSubscribed;

    static CheemsPixelHandButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsPixelHandButton),
            new FrameworkPropertyMetadata(typeof(CheemsPixelHandButton)));
    }

    public CheemsPixelHandButton()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _dotTranslation = null;
        if (GetTemplateChild(PartDotsName) is Rectangle { Fill: DrawingBrush templateBrush } dots)
        {
            var instanceBrush = templateBrush.CloneCurrentValue();
            _dotTranslation = new TranslateTransform();
            instanceBrush.Transform = _dotTranslation;
            dots.Fill = instanceBrush;
        }

        InstallCoinLayer(0, PartRearName);
        InstallCoinLayer(1, PartRearShadowName);
        InstallCoinLayer(2, PartRearHighlightName);
        InstallCoinLayer(3, PartRearShadeName);
        ResetCoinLayers();
        ApplyDotFrame(0);
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        StartCoinBurst();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _animationStartedAt = Stopwatch.GetTimestamp();
        ApplyDotFrame(0);

        if (SystemParameters.ClientAreaAnimation)
        {
            SubscribeRendering();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => UnsubscribeRendering();

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!IsVisible)
        {
            return;
        }

        var elapsed = (Stopwatch.GetTimestamp() - _animationStartedAt) / (double)Stopwatch.Frequency;
        var phase = elapsed / DotAnimationDurationSeconds;
        phase -= Math.Floor(phase);
        ApplyDotFrame(phase);
        ApplyCoinBurstFrame();
    }

    private void ApplyDotFrame(double phase)
    {
        if (_dotTranslation is not null)
        {
            _dotTranslation.X = DotTileSize * phase;
        }
    }

    private void InstallCoinLayer(int index, string partName)
    {
        var layer = GetTemplateChild(partName) as FrameworkElement;
        _coinLayers[index] = layer;
        _coinTranslations[index] = null;

        if (layer is null)
        {
            return;
        }

        var translation = layer.RenderTransform as TranslateTransform;
        translation = translation?.CloneCurrentValue() ?? new TranslateTransform();
        layer.RenderTransform = translation;
        _coinTranslations[index] = translation;
    }

    private void StartCoinBurst()
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            return;
        }

        ResetCoinLayers();
        _coinBurstStartedAt = Stopwatch.GetTimestamp();
        _coinBurstActive = true;
        SubscribeRendering();
    }

    private void ApplyCoinBurstFrame()
    {
        if (!_coinBurstActive)
        {
            return;
        }

        var elapsed = (Stopwatch.GetTimestamp() - _coinBurstStartedAt) / (double)Stopwatch.Frequency;
        var phase = Math.Clamp(elapsed / CoinBurstDurationSeconds, 0, 1);
        var eased = CubicBezierEase(phase, 0, 0.5, 0.4, 1);

        for (var index = 0; index < _coinLayers.Length; index++)
        {
            if (_coinTranslations[index] is not null)
            {
                _coinTranslations[index]!.Y = CoinBurstDistance * eased;
            }

            if (_coinLayers[index] is not null)
            {
                _coinLayers[index]!.Opacity = 1 - eased;
            }
        }

        if (phase >= 1)
        {
            _coinBurstActive = false;
            ResetCoinLayers();
        }
    }

    private void ResetCoinLayers()
    {
        for (var index = 0; index < _coinLayers.Length; index++)
        {
            if (_coinTranslations[index] is not null)
            {
                _coinTranslations[index]!.Y = 0;
            }

            if (_coinLayers[index] is not null)
            {
                _coinLayers[index]!.Opacity = 1;
            }
        }
    }

    private static double CubicBezierEase(double time, double x1, double y1, double x2, double y2)
    {
        var lower = 0.0;
        var upper = 1.0;
        var parameter = time;

        for (var iteration = 0; iteration < 12; iteration++)
        {
            parameter = (lower + upper) / 2;
            if (CubicBezierCoordinate(parameter, x1, x2) < time)
            {
                lower = parameter;
            }
            else
            {
                upper = parameter;
            }
        }

        return CubicBezierCoordinate(parameter, y1, y2);
    }

    private static double CubicBezierCoordinate(double parameter, double control1, double control2)
    {
        var inverse = 1 - parameter;
        return (3 * inverse * inverse * parameter * control1)
               + (3 * inverse * parameter * parameter * control2)
               + (parameter * parameter * parameter);
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
}
