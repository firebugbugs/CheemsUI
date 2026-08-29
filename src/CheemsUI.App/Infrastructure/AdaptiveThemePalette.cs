using System.Windows.Media;

namespace CheemsUI.App.Infrastructure;

/// <summary>
/// Derives a readable application palette from a background effect's representative colour.
/// Surfaces receive a restrained tint, while text and accents preserve contrast.
/// </summary>
internal sealed class AdaptiveThemePalette
{
    private AdaptiveThemePalette(
        Color windowBorder, Color sidebarBackground, Color navigationHover, Color navigationSelected,
        Color primaryText, Color secondaryText, Color onBackgroundPrimaryText, Color onBackgroundSecondaryText,
        Color accent, Color titleBarIconBackground,
        Color titleBarButtonForeground, Color titleBarButtonHover, Color titleBarButtonPressed,
        Color searchBackground, Color searchBorder, Color searchIcon, Color sidebarFooterBackground,
        Color notificationBackground, Color notificationBorder, Color surface, Color elevatedSurface,
        Color border, Color accentText, Color progressTrack)
    {
        WindowBorder = windowBorder;
        SidebarBackground = sidebarBackground;
        NavigationHover = navigationHover;
        NavigationSelected = navigationSelected;
        PrimaryText = primaryText;
        SecondaryText = secondaryText;
        OnBackgroundPrimaryText = onBackgroundPrimaryText;
        OnBackgroundSecondaryText = onBackgroundSecondaryText;
        Accent = accent;
        TitleBarIconBackground = titleBarIconBackground;
        TitleBarButtonForeground = titleBarButtonForeground;
        TitleBarButtonHover = titleBarButtonHover;
        TitleBarButtonPressed = titleBarButtonPressed;
        SearchBackground = searchBackground;
        SearchBorder = searchBorder;
        SearchIcon = searchIcon;
        SidebarFooterBackground = sidebarFooterBackground;
        NotificationBackground = notificationBackground;
        NotificationBorder = notificationBorder;
        Surface = surface;
        ElevatedSurface = elevatedSurface;
        Border = border;
        AccentText = accentText;
        ProgressTrack = progressTrack;
    }

    public Color WindowBorder { get; }
    public Color SidebarBackground { get; }
    public Color NavigationHover { get; }
    public Color NavigationSelected { get; }
    public Color PrimaryText { get; }
    public Color SecondaryText { get; }
    public Color OnBackgroundPrimaryText { get; }
    public Color OnBackgroundSecondaryText { get; }
    public Color Accent { get; }
    public Color TitleBarIconBackground { get; }
    public Color TitleBarButtonForeground { get; }
    public Color TitleBarButtonHover { get; }
    public Color TitleBarButtonPressed { get; }
    public Color SearchBackground { get; }
    public Color SearchBorder { get; }
    public Color SearchIcon { get; }
    public Color SidebarFooterBackground { get; }
    public Color NotificationBackground { get; }
    public Color NotificationBorder { get; }
    public Color Surface { get; }
    public Color ElevatedSurface { get; }
    public Color Border { get; }
    public Color AccentText { get; }
    public Color ProgressTrack { get; }

    public static AdaptiveThemePalette Create(Color source, bool isDark)
    {
        var (hue, saturation, _) = ToHsl(source);
        return saturation < 0.08 ? CreateNeutral(isDark) : CreateTinted(hue, saturation, isDark);
    }

    private static AdaptiveThemePalette CreateTinted(double hue, double saturation, bool isDark)
    {
        Color Tone(double saturationScale, double lightness) => FromHsl(
            hue,
            Math.Clamp(saturation * saturationScale, 0.06, 0.82),
            lightness);

        if (isDark)
        {
            var surface = Tone(0.34, 0.16);
            var elevatedSurface = Tone(0.31, 0.22);
            var primaryText = Tone(0.18, 0.96);
            var secondaryText = Tone(0.16, 0.82);
            var accent = Tone(0.82, 0.78);

            return new AdaptiveThemePalette(
                windowBorder: Tone(0.38, 0.43),
                sidebarBackground: WithAlpha(Tone(0.38, 0.12), 0xD9),
                navigationHover: WithAlpha(Tone(0.56, 0.28), 0xB8),
                navigationSelected: WithAlpha(Tone(0.52, 0.24), 0xD8),
                primaryText: primaryText,
                secondaryText: secondaryText,
                onBackgroundPrimaryText: primaryText,
                onBackgroundSecondaryText: Tone(0.20, 0.91),
                accent: accent,
                titleBarIconBackground: Tone(0.44, 0.18),
                titleBarButtonForeground: primaryText,
                titleBarButtonHover: WithAlpha(Tone(0.48, 0.31), 0xC8),
                titleBarButtonPressed: WithAlpha(Tone(0.54, 0.25), 0xE0),
                searchBackground: WithAlpha(Tone(0.36, 0.13), 0xD0),
                searchBorder: WithAlpha(Tone(0.36, 0.58), 0xA0),
                searchIcon: Tone(0.24, 0.86),
                sidebarFooterBackground: WithAlpha(Tone(0.38, 0.17), 0xC8),
                notificationBackground: surface,
                notificationBorder: Tone(0.36, 0.48),
                surface: WithAlpha(surface, 0xE8),
                elevatedSurface: WithAlpha(elevatedSurface, 0xE8),
                border: WithAlpha(Tone(0.34, 0.63), 0xB8),
                accentText: Tone(0.28, 0.14),
                progressTrack: Tone(0.34, 0.12));
        }

        var lightSurface = Tone(0.14, 0.985);
        var lightElevatedSurface = Tone(0.20, 0.955);
        var darkText = Tone(0.30, 0.17);
        var mutedText = Tone(0.22, 0.37);
        var darkAccent = Tone(0.74, 0.29);

        return new AdaptiveThemePalette(
            windowBorder: Tone(0.25, 0.72),
            sidebarBackground: WithAlpha(Tone(0.44, 0.82), 0xCF),
            navigationHover: WithAlpha(Tone(0.46, 0.79), 0xBF),
            navigationSelected: WithAlpha(Tone(0.30, 0.92), 0xE0),
            primaryText: darkText,
            secondaryText: mutedText,
            onBackgroundPrimaryText: darkText,
            onBackgroundSecondaryText: Tone(0.28, 0.28),
            accent: darkAccent,
            titleBarIconBackground: darkAccent,
            titleBarButtonForeground: Tone(0.22, 0.31),
            titleBarButtonHover: Tone(0.16, 0.87),
            titleBarButtonPressed: Tone(0.20, 0.80),
            searchBackground: WithAlpha(Tone(0.26, 0.91), 0xEA),
            searchBorder: Tone(0.30, 0.72),
            searchIcon: Tone(0.24, 0.38),
            sidebarFooterBackground: WithAlpha(Tone(0.38, 0.77), 0xD4),
            notificationBackground: lightSurface,
            notificationBorder: Tone(0.20, 0.82),
            surface: lightSurface,
            elevatedSurface: lightElevatedSurface,
            border: Tone(0.18, 0.86),
            accentText: Colors.White,
            progressTrack: Tone(0.16, 0.91));
    }

    private static AdaptiveThemePalette CreateNeutral(bool isDark)
    {
        if (isDark)
        {
            return new AdaptiveThemePalette(
                Color.FromRgb(0x5A, 0x70, 0x8B), Color.FromArgb(0xD9, 0x0B, 0x20, 0x3A),
                Color.FromArgb(0x75, 0x4A, 0x68, 0x8A), Color.FromArgb(0xB8, 0x20, 0x3A, 0x5C),
                Color.FromRgb(0xF4, 0xF7, 0xFB), Color.FromRgb(0xC0, 0xCE, 0xDD),
                Color.FromRgb(0xF4, 0xF7, 0xFB), Color.FromRgb(0xD6, 0xE1, 0xEE),
                Color.FromRgb(0x93, 0xD7, 0xFF), Color.FromRgb(0x0D, 0x26, 0x43),
                Color.FromRgb(0xEB, 0xF4, 0xFF), Color.FromArgb(0xC8, 0x4D, 0x69, 0x89),
                Color.FromArgb(0xE0, 0x36, 0x50, 0x6E), Color.FromArgb(0xB8, 0x0B, 0x20, 0x3A),
                Color.FromArgb(0xA0, 0xA8, 0xC0, 0xDA), Color.FromRgb(0xD0, 0xE0, 0xF0),
                Color.FromArgb(0xB8, 0x11, 0x2A, 0x47), Color.FromRgb(0x16, 0x2D, 0x49),
                Color.FromRgb(0x75, 0x91, 0xAF), Color.FromArgb(0xE8, 0x16, 0x2D, 0x49),
                Color.FromArgb(0xE8, 0x20, 0x3B, 0x59), Color.FromArgb(0xB8, 0x86, 0xA3, 0xC1),
                Color.FromRgb(0x0B, 0x20, 0x3A), Color.FromRgb(0x0B, 0x20, 0x3A));
        }

        return new AdaptiveThemePalette(
            Color.FromRgb(0xD1, 0xD1, 0xD1), Color.FromArgb(0xD9, 0xF5, 0xF5, 0xF5),
            Color.FromArgb(0xBF, 0xEC, 0xEC, 0xEC), Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF),
            Color.FromRgb(0x20, 0x21, 0x24), Color.FromRgb(0x74, 0x77, 0x7C),
            Color.FromRgb(0x20, 0x21, 0x24), Color.FromRgb(0x58, 0x5B, 0x5F),
            Color.FromRgb(0x20, 0x21, 0x24), Color.FromRgb(0x20, 0x21, 0x24),
            Color.FromRgb(0x4D, 0x4F, 0x53), Color.FromRgb(0xDC, 0xDC, 0xDC),
            Color.FromRgb(0xD2, 0xD2, 0xD2), Colors.White, Color.FromRgb(0xE0, 0xE0, 0xE0),
            Color.FromRgb(0x77, 0x7A, 0x7F), Color.FromArgb(0xCC, 0xEC, 0xEC, 0xEC),
            Colors.White, Color.FromRgb(0xD6, 0xD9, 0xDE), Colors.White,
            Color.FromRgb(0xF5, 0xF6, 0xFA), Color.FromRgb(0xDF, 0xE6, 0xE9),
            Colors.White, Color.FromRgb(0xE5, 0xEC, 0xF3));
    }

    private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);

    private static (double Hue, double Saturation, double Lightness) ToHsl(Color color)
    {
        var red = color.R / 255d;
        var green = color.G / 255d;
        var blue = color.B / 255d;
        var maximum = Math.Max(red, Math.Max(green, blue));
        var minimum = Math.Min(red, Math.Min(green, blue));
        var lightness = (maximum + minimum) / 2d;
        var delta = maximum - minimum;

        if (delta == 0)
        {
            return (0, 0, lightness);
        }

        var saturation = delta / (1d - Math.Abs(2d * lightness - 1d));
        var hue = maximum == red
            ? 60d * (((green - blue) / delta) % 6d)
            : maximum == green
                ? 60d * (((blue - red) / delta) + 2d)
                : 60d * (((red - green) / delta) + 4d);

        return (hue < 0 ? hue + 360d : hue, saturation, lightness);
    }

    private static Color FromHsl(double hue, double saturation, double lightness)
    {
        var chroma = (1d - Math.Abs(2d * lightness - 1d)) * saturation;
        var huePrime = hue / 60d;
        var second = chroma * (1d - Math.Abs(huePrime % 2d - 1d));
        var (red, green, blue) = huePrime switch
        {
            < 1d => (chroma, second, 0d),
            < 2d => (second, chroma, 0d),
            < 3d => (0d, chroma, second),
            < 4d => (0d, second, chroma),
            < 5d => (second, 0d, chroma),
            _ => (chroma, 0d, second)
        };
        var match = lightness - chroma / 2d;

        return Color.FromRgb(
            (byte)Math.Round((red + match) * 255d),
            (byte)Math.Round((green + match) * 255d),
            (byte)Math.Round((blue + match) * 255d));
    }
}
