using System.Windows;
using CheemsControl.App.Infrastructure;

namespace CheemsControl.App;

/// <summary>
/// 全局异常处理：未处理异常写入 ErrorLog 后保持运行（Demo 不因单点异常退出），
/// 报错详情可从日志文件直接复制。
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            ErrorLog.Write(args.Exception);
            args.Handled = true;
            MessageBox.Show(
                $"发生未处理异常，程序将继续运行。\n详情已写入日志（可直接打开复制）：\n{ErrorLog.LogFilePath}",
                "CheemsControl Demo",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        };
    }
}
