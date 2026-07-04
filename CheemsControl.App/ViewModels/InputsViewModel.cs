using CheemsControl.App.Infrastructure;

namespace CheemsControl.App.ViewModels;

/// <summary>
/// 输入类页面 VM：CheemsDayNightSwitch 的双向绑定示例状态。
/// </summary>
public sealed class InputsViewModel : SearchablePageViewModel
{
    private bool _sunIsOn;

    public InputsViewModel() : base(new Dictionary<string, string>
    {
        ["DayNight"] = "CheemsDayNightSwitch day night switch toggle 日夜 昼夜 太阳 月亮 开关 Mohammad-Rahme-576 uiverse",
        ["AmPm"] = "CheemsAmPmToggle AM PM switch toggle 上午 下午 白天 夜晚 开关 mobinkakei uiverse",
        ["Led"] = "CheemsLedSwitch LED switch toggle 灯 发光 开关 chase2k25 uiverse neumorphism 拟态",
        ["Pixel"] = "CheemsPixelSwitch pixel switch toggle 像素 开关 zl306 uiverse",
        ["Metal"] = "CheemsMetalSwitch metal metallic handle switch toggle 金属 手柄 开关 cssbuttons-io uiverse",
        ["PixelCoin"] = "CheemsPixelCoinSwitch pixel coin happy sad switch toggle 像素 硬币 笑脸 哭脸 开关 santhosh_2608 uiverse",
        ["GlowInput"] = "CheemsGlowInput input textbox text field glow focus hover 输入框 文本框 发光 聚焦 悬停 蓝色 reglobby uiverse",
        ["SearchBox"] = "CheemsSearchBox search input textbox floating label clear 搜索 输入框 文本框 浮动标签 清除 Li-Deheng uiverse"
    })
    {
    }

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

    public bool IsDayNightVisible => IsControlVisible("DayNight");
    public bool IsAmPmVisible => IsControlVisible("AmPm");
    public bool IsLedVisible => IsControlVisible("Led");
    public bool IsPixelVisible => IsControlVisible("Pixel");
    public bool IsMetalVisible => IsControlVisible("Metal");
    public bool IsPixelCoinVisible => IsControlVisible("PixelCoin");
    public bool IsGlowInputVisible => IsControlVisible("GlowInput");
    public bool IsSearchBoxVisible => IsControlVisible("SearchBox");
    public bool IsSwitchSectionVisible => VisibleSwitchCount > 0;
    public bool IsTextBoxSectionVisible => IsGlowInputVisible || IsSearchBoxVisible;
    public int VisibleSwitchCount => new[]
    {
        IsDayNightVisible, IsAmPmVisible, IsLedVisible,
        IsPixelVisible, IsMetalVisible, IsPixelCoinVisible
    }.Count(value => value);

    protected override void OnSearchFilterChanged()
    {
        OnPropertyChanged(nameof(IsDayNightVisible));
        OnPropertyChanged(nameof(IsAmPmVisible));
        OnPropertyChanged(nameof(IsLedVisible));
        OnPropertyChanged(nameof(IsPixelVisible));
        OnPropertyChanged(nameof(IsMetalVisible));
        OnPropertyChanged(nameof(IsPixelCoinVisible));
        OnPropertyChanged(nameof(IsGlowInputVisible));
        OnPropertyChanged(nameof(IsSearchBoxVisible));
        OnPropertyChanged(nameof(IsSwitchSectionVisible));
        OnPropertyChanged(nameof(IsTextBoxSectionVisible));
        OnPropertyChanged(nameof(VisibleSwitchCount));
    }
}
