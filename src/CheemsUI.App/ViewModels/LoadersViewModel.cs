using CheemsUI.App.Infrastructure;

namespace CheemsUI.App.ViewModels;

/// <summary>
/// 加载动画页面 VM。
/// </summary>
public sealed class LoadersViewModel : SearchablePageViewModel
{
    public LoadersViewModel() : base(new Dictionary<string, string>
    {
        ["Typewriter"] = "CheemsTypewriterLoader typewriter loader 打字机 加载 动画 Nawsome uiverse",
        ["WashingMachine"] = "CheemsWashingMachineLoader washing machine washer loader 洗衣机 加载 Shoh2008 uiverse",
        ["AiMatrix"] = "CheemsAiMatrixLoader AI matrix binary digit loader 人工智能 矩阵 二进制 数字 01 加载 PriyanshuGupta28 uiverse",
        ["HamsterWheel"] = "CheemsHamsterWheelLoader hamster wheel loader 仓鼠 跑轮 加载 Nawsome uiverse",
        ["CubeLoading"] = "CheemsCubeLoadingLoader cube loading 3d loading letters 方块 立方体 字母 加载 dexter-st uiverse",
        ["JumpingSquare"] = "CheemsJumpingSquareLoader jumping square loader 跳跃 方块 粉色 加载 alexruix uiverse",
        ["Earth"] = "CheemsEarthLoader earth globe planet connecting loader 地球 全球 星球 连接 网络 加载 Novaxlo uiverse",
        ["NewtonsCradle"] = "CheemsNewtonsCradleLoader Newton Newton's cradle pendulum balls loader 牛顿摆 牛顿球 摆球 加载 dovatgabriel uiverse",
        ["Domino"] = "CheemsDominoLoader domino dominos loader 多米诺 骨牌 绿色 加载 Zadrus uiverse",
        ["WaveBars"] = "CheemsWaveBarsLoader loader3 wave bars equalizer audio music 蓝色 波形 音频 音乐 均衡器 柱状 加载 Satwinder04 uiverse",
        ["Blob"] = "CheemsBlobLoader blob gooey blur contrast loader 黏液 液滴 果冻 模糊 蓝色 加载 vikramsinghnegi uiverse",
        ["RainbowBars"] = "CheemsRainbowBarsLoader rainbow bars equalizer color glow loader 彩虹 彩色 柱状 均衡器 发光 加载 Gianluks90 uiverse",
        ["Glitch"] = "CheemsGlitchLoader glitch text loading loader 故障 文字 错位 抖动 倾斜 紫色 绿色 加载 andrew-demchenk0 uiverse",
        ["Polyline"] = "CheemsPolylineLoader polyline heartbeat pulse ECG loading loader 心跳 心电图 脉搏 折线 虚线 动画 红色 加载 milley69 uiverse",
        ["PulseDots"] = "CheemsPulseDotsLoader dots pulse loading loader five circles 五点 圆点 脉冲 扩散 蓝色 加载 adamgiebl uiverse",
        ["OrbitDots"] = "CheemsOrbitDotsLoader rotating orbit colorful dots flash loader 旋转 轨道 彩色 圆点 闪光 收缩 加载 Nawsome uiverse"
    })
    {
    }

    public bool IsTypewriterVisible => IsControlVisible("Typewriter");
    public bool IsWashingMachineVisible => IsControlVisible("WashingMachine");
    public bool IsAiMatrixVisible => IsControlVisible("AiMatrix");
    public bool IsHamsterWheelVisible => IsControlVisible("HamsterWheel");
    public bool IsCubeLoadingVisible => IsControlVisible("CubeLoading");
    public bool IsJumpingSquareVisible => IsControlVisible("JumpingSquare");
    public bool IsEarthVisible => IsControlVisible("Earth");
    public bool IsNewtonsCradleVisible => IsControlVisible("NewtonsCradle");
    public bool IsDominoVisible => IsControlVisible("Domino");
    public bool IsWaveBarsVisible => IsControlVisible("WaveBars");
    public bool IsBlobVisible => IsControlVisible("Blob");
    public bool IsRainbowBarsVisible => IsControlVisible("RainbowBars");
    public bool IsGlitchVisible => IsControlVisible("Glitch");
    public bool IsPolylineVisible => IsControlVisible("Polyline");
    public bool IsPulseDotsVisible => IsControlVisible("PulseDots");
    public bool IsOrbitDotsVisible => IsControlVisible("OrbitDots");
    protected override void OnSearchFilterChanged()
    {
        OnPropertyChanged(nameof(IsTypewriterVisible));
        OnPropertyChanged(nameof(IsWashingMachineVisible));
        OnPropertyChanged(nameof(IsAiMatrixVisible));
        OnPropertyChanged(nameof(IsHamsterWheelVisible));
        OnPropertyChanged(nameof(IsCubeLoadingVisible));
        OnPropertyChanged(nameof(IsJumpingSquareVisible));
        OnPropertyChanged(nameof(IsEarthVisible));
        OnPropertyChanged(nameof(IsNewtonsCradleVisible));
        OnPropertyChanged(nameof(IsDominoVisible));
        OnPropertyChanged(nameof(IsWaveBarsVisible));
        OnPropertyChanged(nameof(IsBlobVisible));
        OnPropertyChanged(nameof(IsRainbowBarsVisible));
        OnPropertyChanged(nameof(IsGlitchVisible));
        OnPropertyChanged(nameof(IsPolylineVisible));
        OnPropertyChanged(nameof(IsPulseDotsVisible));
        OnPropertyChanged(nameof(IsOrbitDotsVisible));
    }
}
