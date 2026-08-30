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
        ["Dots"] = "CheemsDotsBackground Vanta DOTS three.js dots points lines grid mouse interactive background WebGL 点阵 连线 网格 鼠标 交互 背景 特效 离线",
        ["Cells"] = "CheemsCellsBackground Vanta CELLS three.js cells organic cellular mouse touch interactive background WebGL 细胞 有机 鼠标 触摸 交互 背景 特效 离线",
        ["RisoDither"] = "CheemsRisoDitherBackground Riso Dither AIDesigner WebGL Bayer ordered dither risograph 网点 印刷 抖动 Bayer WebGL 背景 特效 离线"
    })
    {
        Birds = new("Birds", "Birds 鸟群", "#FF3F81");
        Clouds = new("Clouds", "Clouds 云层", "#FFFFFF");
        Dots = new("Dots", "Dots 点阵", "#FF8820");
        Cells = new("Cells", "Cells 细胞", "#A4E34F");
        RisoDither = new("RisoDither", "Riso Dither", "#8B5CF6")
        {
            RisoBackgroundAlpha = 1,
            AnimationSpeed = 0.73,
            RisoPixelSize = 9,
            RisoLevels = 12,
            RisoScale = 2.34,
            RisoContrast = 1.78,
            RisoFlowAngle = 97,
            RisoDetail = 0.54,
            RisoGlow = 0.77
        };
        Profiles = [Birds, Clouds, Dots, Cells, RisoDither];
        foreach (var profile in Profiles)
        {
            profile.PropertyChanged += (_, _) => SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? SettingsChanged;

    public BackgroundProfileViewModel Birds { get; }
    public BackgroundProfileViewModel Clouds { get; }
    public BackgroundProfileViewModel Dots { get; }
    public BackgroundProfileViewModel Cells { get; }
    public BackgroundProfileViewModel RisoDither { get; }
    public IReadOnlyList<BackgroundProfileViewModel> Profiles { get; }

    public BackgroundProfileViewModel? GetProfile(string key) =>
        Profiles.FirstOrDefault(profile => string.Equals(profile.Key, key, StringComparison.Ordinal));

    public bool IsBirdsVisible => IsControlVisible("Birds");

    public bool IsCloudsVisible => IsControlVisible("Clouds");

    public bool IsDotsVisible => IsControlVisible("Dots");

    public bool IsCellsVisible => IsControlVisible("Cells");

    public bool IsRisoDitherVisible => IsControlVisible("RisoDither");

    public bool IsBackgroundsVisible => IsBirdsVisible || IsCloudsVisible || IsDotsVisible || IsCellsVisible || IsRisoDitherVisible;

    protected override void OnSearchFilterChanged()
    {
        OnPropertyChanged(nameof(IsBirdsVisible));
        OnPropertyChanged(nameof(IsCloudsVisible));
        OnPropertyChanged(nameof(IsDotsVisible));
        OnPropertyChanged(nameof(IsCellsVisible));
        OnPropertyChanged(nameof(IsRisoDitherVisible));
        OnPropertyChanged(nameof(IsBackgroundsVisible));
    }
}
