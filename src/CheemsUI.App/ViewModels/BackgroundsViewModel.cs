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
        ["RisoDither"] = "CheemsRisoDitherBackground Riso Dither AIDesigner WebGL Bayer ordered dither risograph 网点 印刷 抖动 Bayer WebGL 背景 特效 离线",
        ["Cubes"] = "CheemsCubesBackground Cubes 立方体 conic-gradient Uiverse csemszepp 静态 背景 几何 拼贴 图案 方块 等距 原生",
        ["Matrix"] = "CheemsMatrixRainBackground Matrix 数字雨 代码雨 雨帘 黑客帝国 片假名 绿色 字符 动态 背景 Uiverse whoisyourdeadie 原生"
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
        Cubes = new("Cubes", "Cubes 立方体", "#E19989");
        // PrimaryColor 仅用于深色背景下的自适应配色感知色，数字雨颜色保持源码原值不开放。
        Matrix = new("Matrix", "Matrix 数字雨", "#00FF41");
        Profiles = [Birds, Clouds, Dots, Cells, RisoDither, Cubes, Matrix];
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

    // PrimaryColor 仅作窗口自适应配色的感知色（四色按面积加权的混合值），不对用户开放。
    public BackgroundProfileViewModel Cubes { get; }

    public BackgroundProfileViewModel Matrix { get; }
    public IReadOnlyList<BackgroundProfileViewModel> Profiles { get; }

    public BackgroundProfileViewModel? GetProfile(string key) =>
        Profiles.FirstOrDefault(profile => string.Equals(profile.Key, key, StringComparison.Ordinal));

    public bool IsBirdsVisible => IsControlVisible("Birds");

    public bool IsCloudsVisible => IsControlVisible("Clouds");

    public bool IsDotsVisible => IsControlVisible("Dots");

    public bool IsCellsVisible => IsControlVisible("Cells");

    public bool IsRisoDitherVisible => IsControlVisible("RisoDither");

    public bool IsCubesVisible => IsControlVisible("Cubes");

    public bool IsMatrixVisible => IsControlVisible("Matrix");

    public bool IsBackgroundsVisible => IsBirdsVisible || IsCloudsVisible || IsDotsVisible || IsCellsVisible || IsRisoDitherVisible || IsCubesVisible || IsMatrixVisible;

    protected override void OnSearchFilterChanged()
    {
        OnPropertyChanged(nameof(IsBirdsVisible));
        OnPropertyChanged(nameof(IsCloudsVisible));
        OnPropertyChanged(nameof(IsDotsVisible));
        OnPropertyChanged(nameof(IsCellsVisible));
        OnPropertyChanged(nameof(IsRisoDitherVisible));
        OnPropertyChanged(nameof(IsCubesVisible));
        OnPropertyChanged(nameof(IsMatrixVisible));
        OnPropertyChanged(nameof(IsBackgroundsVisible));
    }
}
