using System.Windows;
using CheemsControl.App.ViewModels;

namespace CheemsControl.App;

/// <summary>
/// 导航壳（规矩 M1）：code-behind 只挂 DataContext，不含业务。
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
