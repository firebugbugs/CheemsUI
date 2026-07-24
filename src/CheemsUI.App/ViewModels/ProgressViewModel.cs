using System.Globalization;
using CheemsUI.App.Infrastructure;

namespace CheemsUI.App.ViewModels;

public sealed class ProgressViewModel : SearchablePageViewModel
{
    private double _progressValue = 40;
    private string _progressInput = "40.0";

    public ProgressViewModel() : base(new Dictionary<string, string>
    {
        ["CosmicProgress"] = "CheemsCosmicProgressBar progress bar cosmic particles ripple neon 星空 粒子 波纹 霓虹 进度条 rust_1966 uiverse",
        ["WaveProgressBall"] = "CheemsWaveProgressBall progress ball wave liquid readonly 波浪 液体 进度球 只读 mrhyddenn uiverse",
        ["MonoProgress"] = "CheemsMonoProgressBar progress bar black white draggable percentage 黑白 可拖动 百分比 thekuntal49 uiverse",
        ["CircuitProgress"] = "CheemsCircuitProgressBar progress bar 3d groove circuit hover tilt 分段 三维 沟槽 悬停 变形 AshtonLiou uiverse"
    })
    {
    }

    public bool IsCosmicProgressVisible => IsControlVisible("CosmicProgress");

    public bool IsWaveProgressBallVisible => IsControlVisible("WaveProgressBall");

    public bool IsMonoProgressVisible => IsControlVisible("MonoProgress");

    public bool IsCircuitProgressVisible => IsControlVisible("CircuitProgress");

    /// <summary>
    /// Progress 页面中所有进度条共享的演示值。
    /// </summary>
    public double ProgressValue
    {
        get => _progressValue;
        set => SetProgressValue(value, synchronizeInput: true);
    }

    public string ProgressInput
    {
        get => _progressInput;
        set
        {
            if (!SetProperty(ref _progressInput, value))
            {
                return;
            }

            if (TryParseProgress(value, out var progress))
            {
                SetProgressValue(progress, synchronizeInput: false);
            }
        }
    }

    public void CommitProgressInput()
    {
        if (TryParseProgress(ProgressInput, out var progress))
        {
            SetProgressValue(progress, synchronizeInput: false);
        }

        SynchronizeProgressInput();
    }

    public void AdjustProgress(double delta)
    {
        ProgressValue += delta;
    }

    protected override void OnSearchFilterChanged()
    {
        OnPropertyChanged(nameof(IsCosmicProgressVisible));
        OnPropertyChanged(nameof(IsWaveProgressBallVisible));
        OnPropertyChanged(nameof(IsMonoProgressVisible));
        OnPropertyChanged(nameof(IsCircuitProgressVisible));
    }

    private static double NormalizeProgress(double value) =>
        Math.Clamp(Math.Round(value, 1, MidpointRounding.AwayFromZero), 0, 100);

    private void SetProgressValue(double value, bool synchronizeInput)
    {
        if (!double.IsFinite(value) || !SetProperty(ref _progressValue, NormalizeProgress(value), nameof(ProgressValue)))
        {
            return;
        }

        if (synchronizeInput)
        {
            SynchronizeProgressInput();
        }
    }

    private void SynchronizeProgressInput()
    {
        _progressInput = ProgressValue.ToString("0.0", CultureInfo.CurrentCulture);
        OnPropertyChanged(nameof(ProgressInput));
    }

    private static bool TryParseProgress(string text, out double value)
    {
        var parsed = double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
                     || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        return parsed && double.IsFinite(value);
    }
}
