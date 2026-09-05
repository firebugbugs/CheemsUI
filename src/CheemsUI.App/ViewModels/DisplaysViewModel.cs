using System.Windows.Threading;

namespace CheemsUI.App.ViewModels;

public sealed class DisplaysViewModel : SearchablePageViewModel
{
    private readonly DispatcherTimer _clockTimer;
    private DateTime _systemTime = DateTime.Now;

    public DisplaysViewModel() : base(new Dictionary<string, string>
    {
        ["FlipClock"] = "CheemsFlipClock minimal flip clock system time 时钟 时间 翻页 数字 wen-yan codepen"
    })
    {
        _clockTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _clockTimer.Tick += (_, _) => RefreshSystemTime();
        _clockTimer.Start();
    }

    public DateTime SystemTime
    {
        get => _systemTime;
        private set => SetProperty(ref _systemTime, value);
    }

    public bool IsFlipClockVisible => IsControlVisible("FlipClock");

    protected override void OnSearchFilterChanged() => OnPropertyChanged(nameof(IsFlipClockVisible));

    private void RefreshSystemTime()
    {
        var now = DateTime.Now;
        if (now.Hour == SystemTime.Hour && now.Minute == SystemTime.Minute && now.Second == SystemTime.Second) return;
        SystemTime = now;
    }
}
