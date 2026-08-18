using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using CheemsUI.App.ViewModels;

namespace CheemsUI.App;

/// <summary>
/// 导航壳（规矩 M1）：code-behind 只挂 DataContext，不含业务。
/// </summary>
public partial class MainWindow : Window
{
    private static readonly Brush DefaultWindowBackground = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8));

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    public void ApplyBirdsBackground()
    {
        PartBirdsWindowBackground.Visibility = Visibility.Visible;
        WindowFrame.Background = Brushes.Transparent;
        Background = Brushes.Transparent;
    }

    public void RestoreDefaultBackground()
    {
        PartBirdsWindowBackground.Visibility = Visibility.Collapsed;
        WindowFrame.Background = DefaultWindowBackground;
        Background = DefaultWindowBackground;
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (PartBirdsWindowBackground.Visibility == Visibility.Visible)
        {
            PartBirdsWindowBackground.SetPointerPosition(e.GetPosition(PartBirdsWindowBackground));
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        SystemCommands.MinimizeWindow(this);
    }

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(this);
            return;
        }

        SystemCommands.MaximizeWindow(this);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        SystemCommands.CloseWindow(this);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.CancelGifExport();
        }

        base.OnClosed(e);
    }
}
