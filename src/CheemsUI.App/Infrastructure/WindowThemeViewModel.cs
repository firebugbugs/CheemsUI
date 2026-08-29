using System.Windows.Media;

namespace CheemsUI.App.Infrastructure;

/// <summary>
/// Shared palette for auxiliary windows and popup surfaces. The main window owns one
/// instance and updates it whenever its background changes.
/// </summary>
internal sealed class WindowThemeViewModel : ObservableObject
{
    private Brush _surfaceBrush = Brushes.White;
    private Brush _borderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xE1, 0xEA));
    private Brush _primaryTextBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
    private Brush _secondaryTextBrush = new SolidColorBrush(Color.FromRgb(0x53, 0x61, 0x74));
    private Brush _mutedSurfaceBrush = new SolidColorBrush(Color.FromRgb(0xF2, 0xF5, 0xF8));
    private Brush _hoverBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xF1, 0xFF));
    private Brush _accentBrush = new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0xEB));
    private Brush _accentTextBrush = Brushes.White;
    private Brush _progressTrackBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0xEC, 0xF3));

    public Brush SurfaceBrush { get => _surfaceBrush; private set => SetProperty(ref _surfaceBrush, value); }
    public Brush BorderBrush { get => _borderBrush; private set => SetProperty(ref _borderBrush, value); }
    public Brush PrimaryTextBrush { get => _primaryTextBrush; private set => SetProperty(ref _primaryTextBrush, value); }
    public Brush SecondaryTextBrush { get => _secondaryTextBrush; private set => SetProperty(ref _secondaryTextBrush, value); }
    public Brush MutedSurfaceBrush { get => _mutedSurfaceBrush; private set => SetProperty(ref _mutedSurfaceBrush, value); }
    public Brush HoverBrush { get => _hoverBrush; private set => SetProperty(ref _hoverBrush, value); }
    public Brush AccentBrush { get => _accentBrush; private set => SetProperty(ref _accentBrush, value); }
    public Brush AccentTextBrush { get => _accentTextBrush; private set => SetProperty(ref _accentTextBrush, value); }
    public Brush ProgressTrackBrush { get => _progressTrackBrush; private set => SetProperty(ref _progressTrackBrush, value); }

    public void Apply(AdaptiveThemePalette palette)
    {
        Apply(
            surface: palette.Surface, border: palette.Border,
            primaryText: palette.PrimaryText, secondaryText: palette.SecondaryText,
            mutedSurface: palette.ElevatedSurface, hover: palette.NavigationHover,
            accent: palette.Accent, accentText: palette.AccentText,
            progressTrack: palette.ProgressTrack);
    }

    private void Apply(Color surface, Color border, Color primaryText, Color secondaryText, Color mutedSurface,
                       Color hover, Color accent, Color accentText, Color progressTrack)
    {
        SurfaceBrush = new SolidColorBrush(surface);
        BorderBrush = new SolidColorBrush(border);
        PrimaryTextBrush = new SolidColorBrush(primaryText);
        SecondaryTextBrush = new SolidColorBrush(secondaryText);
        MutedSurfaceBrush = new SolidColorBrush(mutedSurface);
        HoverBrush = new SolidColorBrush(hover);
        AccentBrush = new SolidColorBrush(accent);
        AccentTextBrush = new SolidColorBrush(accentText);
        ProgressTrackBrush = new SolidColorBrush(progressTrack);
    }
}
