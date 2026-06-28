using System.IO;

namespace CheemsControl.App.Infrastructure;

/// <summary>
/// 未处理异常日志：追加写入 exe 同级 logs/error.log。
/// 配合 App 的 DispatcherUnhandledException 使用，保证任何报错都可从日志文件直接复制。
/// </summary>
internal static class ErrorLog
{
    public static string LogFilePath { get; } = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "logs", "error.log");

    public static void Write(Exception exception)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
            File.AppendAllText(
                LogFilePath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]{Environment.NewLine}{exception}{Environment.NewLine}{new string('=', 80)}{Environment.NewLine}");
        }
        catch
        {
            // 日志失败时保持静默，不能因记录异常再抛异常
        }
    }
}
