using CheemsControl.App.Infrastructure;

namespace CheemsControl.App.ViewModels;

/// <summary>
/// 输入类页面 VM：CheemsDayNightSwitch 的双向绑定示例状态。
/// </summary>
public class InputsViewModel : ObservableObject
{
    private bool _sunIsOn;

    /// <summary>演示用开关状态（TwoWay 绑定到 CheemsDayNightSwitch.IsChecked）。</summary>
    public bool SunIsOn
    {
        get => _sunIsOn;
        set
        {
            if (SetProperty(ref _sunIsOn, value))
            {
                OnPropertyChanged(nameof(SunStateText));
            }
        }
    }

    public string SunStateText => SunIsOn ? "当前：夜晚 🌙" : "当前：白天 ☀";
}
