using System.Windows;

namespace CheemsUI;

/// <summary>
/// C# 侧资源键常量。代码中引用资源一律经此类，禁止裸写字符串键（CONVENTIONS N6）。
/// 键值必须与 Themes/Basic/*.xaml 中定义保持一致。
/// </summary>
public static class CheemsKeys
{
    // ---- 画刷（Brushes.xaml） ----
    public const string PrimaryBrush = "Cheems.Brush.Primary";
    public const string AccentBrush = "Cheems.Brush.Accent";
    public const string TextPrimaryBrush = "Cheems.Brush.Text.Primary";
    public const string TextSecondaryBrush = "Cheems.Brush.Text.Secondary";
    public const string BackgroundDefaultBrush = "Cheems.Brush.Background.Default";
    public const string BackgroundElevatedBrush = "Cheems.Brush.Background.Elevated";
    public const string BorderDefaultBrush = "Cheems.Brush.Border.Default";

    // ---- CheemsDayNightSwitch 颜色（Colors.xaml） ----
    public const string DayNightSunColor = "Cheems.Color.DayNight.Sun";
    public const string DayNightMoonColor = "Cheems.Color.DayNight.Moon";
    public const string DayNightCloudColor = "Cheems.Color.DayNight.Cloud";
    public const string DayNightCraterStrongColor = "Cheems.Color.DayNight.Crater.Strong";
    public const string DayNightCraterFaintColor = "Cheems.Color.DayNight.Crater.Faint";

    // ---- CheemsCubeLoadingLoader 颜色（Colors.xaml） ----
    public const string CubeLoadingHighlightColor = "Cheems.Color.CubeLoading.Highlight";

    // ---- CheemsGlowInput 颜色（Colors.xaml） ----
    public const string GlowInputBackgroundColor = "Cheems.Color.GlowInput.Background";
    public const string GlowInputActiveBackgroundColor = "Cheems.Color.GlowInput.ActiveBackground";
    public const string GlowInputAccentColor = "Cheems.Color.GlowInput.Accent";
    public const string GlowInputTransparentBorderColor = "Cheems.Color.GlowInput.TransparentBorder";

    // ---- CheemsCheckToggle 画刷（Brushes.xaml） ----
    public const string CheckToggleThumbOffBrush = "Cheems.Brush.CheckToggle.ThumbOff";
    public const string CheckToggleThumbOnBrush = "Cheems.Brush.CheckToggle.ThumbOn";

    // ---- 字体（Fonts.xaml） ----
    public const string FontFamilyDefault = "Cheems.FontFamily.Default";
    public const string FontFamilyMono = "Cheems.FontFamily.Mono";
    public const string FontFamilyIcon = "Cheems.FontFamily.Icon";

    public const string FontSizeCaption = "Cheems.FontSize.Caption";
    public const string FontSizeBody = "Cheems.FontSize.Body";
    public const string FontSizeSubTitle = "Cheems.FontSize.SubTitle";
    public const string FontSizeTitle = "Cheems.FontSize.Title";
    public const string FontSizeLarge = "Cheems.FontSize.Large";
}
