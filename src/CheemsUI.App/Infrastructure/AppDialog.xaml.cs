using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CheemsUI.App.Infrastructure;

internal enum AppDialogKind { Information, Warning, Error, Question }

internal sealed record AppDialogOptions(
    string Title,
    string Message,
    AppDialogKind Kind = AppDialogKind.Information,
    string PrimaryButtonText = "确定",
    string? SecondaryButtonText = null);

/// <summary>Reusable modal prompt used for application-level messages and decisions.</summary>
internal partial class AppDialog : Window
{
    private AppDialog(WindowThemeViewModel theme, AppDialogOptions options)
    {
        DataContext = theme;
        InitializeComponent();
        PartTitle.Text = options.Title;
        PartMessage.Text = options.Message;
        PartPrimaryButton.Content = options.PrimaryButtonText;
        PartSecondaryButton.Visibility = string.IsNullOrWhiteSpace(options.SecondaryButtonText)
            ? Visibility.Collapsed
            : Visibility.Visible;
        PartSecondaryButton.Content = options.SecondaryButtonText;
        ApplyKind(options.Kind);
    }

    public static bool Show(Window? owner, WindowThemeViewModel theme, AppDialogOptions options)
    {
        var dialog = new AppDialog(theme, options) { Owner = owner };
        return dialog.ShowDialog() == true;
    }

    private void ApplyKind(AppDialogKind kind)
    {
        var (glyph, colour) = kind switch
        {
            AppDialogKind.Warning => ("!", Color.FromRgb(0xD9, 0x77, 0x06)),
            AppDialogKind.Error => ("×", Color.FromRgb(0xDC, 0x26, 0x26)),
            AppDialogKind.Question => ("?", Color.FromRgb(0x7C, 0x3A, 0xED)),
            _ => ("i", Color.FromRgb(0x1F, 0x6F, 0xEB))
        };
        PartIcon.Text = glyph;
        if (PartIcon.Parent is Border iconBackground)
        {
            iconBackground.Background = new SolidColorBrush(colour);
        }
    }

    private void PrimaryButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void SecondaryButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
