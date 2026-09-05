using CheemsUI.App.Infrastructure;

namespace CheemsUI.App.ViewModels;

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
        ["CheckToggle"] = "CheemsCheckToggle check close toggle checkbox 勾选 关闭 对勾 叉号 开关 mobinkakei uiverse",
        ["ScaleSwitch"] = "CheemsScaleSwitch circle scale rotate toggle checkbox 圆形 缩放 旋转 绿色 开关 Praashoo7 uiverse",
        ["MechanicalToggle"] = "CheemsMechanicalToggle mechanical neumorphic toggle switch 机械 拟态 发光 导轨 开关 tunminh_6850 uiverse",
        ["IosStretchSwitch"] = "CheemsIosStretchSwitch iOS active stretch bigger toggle switch 苹果 按压 延展 开关 sayborduu uiverse",
        ["GenderToggle"] = "CheemsGenderToggle gender female male symbol toggle switch 性别 女性 男性 符号 开关 anand_4957 uiverse",
        ["Led"] = "CheemsLedSwitch LED switch toggle 灯 发光 开关 chase2k25 uiverse neumorphism 拟态",
        ["Pixel"] = "CheemsPixelSwitch pixel switch toggle 像素 开关 zl306 uiverse",
        ["Metal"] = "CheemsMetalSwitch metal metallic handle switch toggle 金属 手柄 开关 cssbuttons-io uiverse",
        ["PixelCoin"] = "CheemsPixelCoinSwitch pixel coin happy sad switch toggle 像素 硬币 笑脸 哭脸 开关 santhosh_2608 uiverse",
        ["RotarySwitch"] = "CheemsRotarySwitch rotary selector multi position gear dial 旋钮 旋转 多档 挡位 开关 Pradeepsaranbishnoi uiverse",
        ["FaceSwitch"] = "CheemsFaceSwitch face expression pink gradient toggle checkbox 表情 粉色 渐变 开关 Shoh2008 uiverse",
        ["TrafficLightSwitch"] = "CheemsTrafficLightSwitch red yellow green radio light selector 红绿灯 红灯 黄灯 绿灯 单选 Praashoo7 uiverse",
        ["DarkTrafficLightSwitch"] = "CheemsDarkTrafficLightSwitch red yellow green dark inactive neutral radio light selector 暗色 红绿灯 熄灭 无颜色 单选 PauloRFJ uiverse",
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
    public bool IsCheckToggleVisible => IsControlVisible("CheckToggle");
    public bool IsScaleSwitchVisible => IsControlVisible("ScaleSwitch");
    public bool IsMechanicalToggleVisible => IsControlVisible("MechanicalToggle");
    public bool IsIosStretchSwitchVisible => IsControlVisible("IosStretchSwitch");
    public bool IsGenderToggleVisible => IsControlVisible("GenderToggle");
    public bool IsLedVisible => IsControlVisible("Led");
    public bool IsPixelVisible => IsControlVisible("Pixel");
    public bool IsMetalVisible => IsControlVisible("Metal");
    public bool IsPixelCoinVisible => IsControlVisible("PixelCoin");
    public bool IsRotarySwitchVisible => IsControlVisible("RotarySwitch");
    public bool IsFaceSwitchVisible => IsControlVisible("FaceSwitch");
    public bool IsTrafficLightSwitchVisible => IsControlVisible("TrafficLightSwitch");
    public bool IsDarkTrafficLightSwitchVisible => IsControlVisible("DarkTrafficLightSwitch");
    public bool IsGlowInputVisible => IsControlVisible("GlowInput");
    public bool IsSearchBoxVisible => IsControlVisible("SearchBox");
    public bool IsSwitchSectionVisible => VisibleSwitchCount > 0;
    public bool IsTextBoxSectionVisible => IsGlowInputVisible || IsSearchBoxVisible;
    public int VisibleSwitchCount => new[]
    {
        IsDayNightVisible, IsAmPmVisible, IsCheckToggleVisible, IsScaleSwitchVisible, IsMechanicalToggleVisible, IsIosStretchSwitchVisible, IsGenderToggleVisible, IsLedVisible,
        IsPixelVisible, IsMetalVisible, IsPixelCoinVisible, IsRotarySwitchVisible, IsFaceSwitchVisible, IsTrafficLightSwitchVisible, IsDarkTrafficLightSwitchVisible
    }.Count(value => value);

    protected override void OnSearchFilterChanged()
    {
        OnPropertyChanged(nameof(IsDayNightVisible));
        OnPropertyChanged(nameof(IsAmPmVisible));
        OnPropertyChanged(nameof(IsCheckToggleVisible));
        OnPropertyChanged(nameof(IsScaleSwitchVisible));
        OnPropertyChanged(nameof(IsMechanicalToggleVisible));
        OnPropertyChanged(nameof(IsIosStretchSwitchVisible));
        OnPropertyChanged(nameof(IsGenderToggleVisible));
        OnPropertyChanged(nameof(IsLedVisible));
        OnPropertyChanged(nameof(IsPixelVisible));
        OnPropertyChanged(nameof(IsMetalVisible));
        OnPropertyChanged(nameof(IsPixelCoinVisible));
        OnPropertyChanged(nameof(IsRotarySwitchVisible));
        OnPropertyChanged(nameof(IsFaceSwitchVisible));
        OnPropertyChanged(nameof(IsTrafficLightSwitchVisible));
        OnPropertyChanged(nameof(IsDarkTrafficLightSwitchVisible));
        OnPropertyChanged(nameof(IsGlowInputVisible));
        OnPropertyChanged(nameof(IsSearchBoxVisible));
        OnPropertyChanged(nameof(IsSwitchSectionVisible));
        OnPropertyChanged(nameof(IsTextBoxSectionVisible));
        OnPropertyChanged(nameof(VisibleSwitchCount));
    }
}
