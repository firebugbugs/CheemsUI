using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace CheemsUI.App.Infrastructure;

internal partial class AboutWindow : Window
{
    public AboutWindow(Window owner, WindowThemeViewModel theme)
    {
        Owner = owner;
        DataContext = theme;
        InitializeComponent();
        var version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "0.0.0";
        PartVersion.Text = $"版本 {version}";
    }

    private void GiteeLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
