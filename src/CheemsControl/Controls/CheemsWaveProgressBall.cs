using System.Windows;
using System.Windows.Controls;

namespace CheemsControl;

/// <summary>
/// Uiverse mrhyddenn 波浪进度球的只读 WPF 等价实现。
/// </summary>
[TemplatePart(Name = PartWaveOneName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartWaveTwoName, Type = typeof(FrameworkElement))]
public sealed class CheemsWaveProgressBall : ProgressBar
{
    private const string PartWaveOneName = "PartWaveOne";
    private const string PartWaveTwoName = "PartWaveTwo";
    private const double LiquidSize = 80;
    private const double WaveSize = 160;

    private FrameworkElement? _waveOne;
    private FrameworkElement? _waveTwo;

    static CheemsWaveProgressBall()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsWaveProgressBall),
            new FrameworkPropertyMetadata(typeof(CheemsWaveProgressBall)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _waveOne = GetTemplateChild(PartWaveOneName) as FrameworkElement;
        _waveTwo = GetTemplateChild(PartWaveTwoName) as FrameworkElement;
        UpdateWaterLevel();
    }

    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        UpdateWaterLevel();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == MinimumProperty || e.Property == MaximumProperty)
        {
            UpdateWaterLevel();
        }
    }

    private void UpdateWaterLevel()
    {
        var range = Maximum - Minimum;
        var progress = range <= 0 ? 0 : Math.Clamp((Value - Minimum) / range, 0, 1);
        var waveTop = (LiquidSize * (1 - progress)) - WaveSize;
        var waveVisibility = progress >= 1 - 0.0001
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (_waveOne is not null)
        {
            Canvas.SetTop(_waveOne, waveTop);
            _waveOne.Visibility = waveVisibility;
        }

        if (_waveTwo is not null)
        {
            Canvas.SetTop(_waveTwo, waveTop);
            _waveTwo.Visibility = waveVisibility;
        }
    }
}
