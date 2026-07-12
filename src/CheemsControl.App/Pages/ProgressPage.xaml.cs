using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CheemsControl.App.ViewModels;

namespace CheemsControl.App.Pages;

public partial class ProgressPage : UserControl
{
    public ProgressPage()
    {
        InitializeComponent();
    }

    private void ProgressInput_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (DataContext is ProgressViewModel viewModel)
        {
            viewModel.CommitProgressInput();
        }
    }

    private void ProgressInput_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ProgressViewModel viewModel)
        {
            return;
        }

        if (e.Key == Key.Up || e.Key == Key.Down)
        {
            viewModel.AdjustProgress(e.Key == Key.Up ? 0.1 : -0.1);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            viewModel.CommitProgressInput();
            Keyboard.ClearFocus();
            e.Handled = true;
        }
    }
}
