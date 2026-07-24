using System.Windows;
using CheemsUI.App.Infrastructure;

namespace CheemsUI.App;

/// <summary>
/// 全局异常处理：未处理异常写入 ErrorLog 后保持运行（Demo 不因单点异常退出），
/// 报错详情可从日志文件直接复制。
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 导出模式（--export）不能弹模态框：无人点击等于永久卡死批处理流程，只记日志继续。
        var isExportMode = e.Args.Contains("--export", StringComparer.OrdinalIgnoreCase);

        DispatcherUnhandledException += (_, args) =>
        {
            ErrorLog.Write(args.Exception);
            args.Handled = true;
            if (isExportMode) return;
            MessageBox.Show(
                $"发生未处理异常，程序将继续运行。\n详情已写入日志（可直接打开复制）：\n{ErrorLog.LogFilePath}",
                "CheemsUI Demo",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        };

        // 启动主窗口（StartupUri 已移除，由 Program.cs 控制入口）
        if (e.Args.Length == 0)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
