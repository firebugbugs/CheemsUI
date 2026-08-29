namespace CheemsUI.App.ViewModels;

/// <summary>
/// 背景特效页面 VM。背景层不是传统控件，但仍接入统一搜索体验。
/// </summary>
public sealed class BackgroundsViewModel : SearchablePageViewModel
{
    public BackgroundsViewModel() : base(new Dictionary<string, string>
    {
        ["Birds"] = "CheemsBirdsBackground Vanta BIRDS three.js birds flock flying mouse interactive background WebGL 鸟群 飞鸟 鼠标 交互 背景 特效 离线",
        ["Clouds"] = "CheemsCloudsBackground Vanta CLOUDS three.js clouds sky mouse touch interactive background WebGL 云层 天空 鼠标 触摸 交互 背景 特效 离线",
        ["Cells"] = "CheemsCellsBackground Vanta CELLS three.js cells organic cellular mouse touch interactive background WebGL 细胞 有机 鼠标 触摸 交互 背景 特效 离线",
        ["RisoDither"] = "CheemsRisoDitherBackground Riso Dither AIDesigner WebGL Bayer ordered dither risograph 网点 印刷 抖动 Bayer WebGL 背景 特效 离线"
    })
    {
    }

    public bool IsBirdsVisible => IsControlVisible("Birds");

    public bool IsCloudsVisible => IsControlVisible("Clouds");

    public bool IsCellsVisible => IsControlVisible("Cells");

    public bool IsRisoDitherVisible => IsControlVisible("RisoDither");

    public bool IsBackgroundsVisible => IsBirdsVisible || IsCloudsVisible || IsCellsVisible || IsRisoDitherVisible;

    protected override void OnSearchFilterChanged()
    {
        OnPropertyChanged(nameof(IsBirdsVisible));
        OnPropertyChanged(nameof(IsCloudsVisible));
        OnPropertyChanged(nameof(IsCellsVisible));
        OnPropertyChanged(nameof(IsRisoDitherVisible));
        OnPropertyChanged(nameof(IsBackgroundsVisible));
    }
}
