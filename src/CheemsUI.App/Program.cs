using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using CheemsUI.App.Infrastructure;

namespace CheemsUI.App;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "--export", StringComparison.OrdinalIgnoreCase))
        {
            return RunExport(args);
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
        return 0;
    }

    private static int RunExport(string[] args)
    {
        TryAttachParentConsole();

        // 默认路径为仓库内固定图库（docs/gallery），带自定义路径参数时输出到指定目录且不清理旧文件
        var useDefaultDir = !(args.Length > 1 && !args[1].StartsWith("--"));
        var outputDir = useDefaultDir
            ? ControlGifExporter.GetDefaultOutputDirectory()
            : args[1];

        var limit = 0;
        var workers = ControlGifExporter.DefaultWorkerCount;
        string? only = null;
        foreach (var arg in args)
        {
            if (arg.StartsWith("--limit="))
                int.TryParse(arg.Substring(8), out limit);
            else if (arg.StartsWith("--workers=", StringComparison.OrdinalIgnoreCase))
                int.TryParse(arg.Substring(10), out workers);
            else if (arg.StartsWith("--only=", StringComparison.OrdinalIgnoreCase))
                only = arg.Substring(7);
        }

        Console.WriteLine("CheemsUI Export");
        Console.WriteLine($"Output: {outputDir}");
        if (limit > 0) Console.WriteLine($"Limit: {limit} controls");
        if (only is not null) Console.WriteLine($"Only: {only}");
        if (workers != 1) Console.WriteLine($"Workers: {workers} (parallel recording)");
        Console.WriteLine();

        var exitCode = 0;
        var tcs = new TaskCompletionSource<int>();
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        var app = new App();
        app.InitializeComponent();

        // 导出时宿主窗口不断开关，必须禁用 OnLastWindowClose：
        // 否则第一个窗口关闭后（此时窗口数为 0）Application 会 Shutdown 掉 Dispatcher，
        // 导出循环里所有 await 的续延被丢弃，tcs.Task.Result 永远等不到结果而卡死。
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        app.Startup += async (_, _) =>
        {
            try
            {
                var exporter = new ControlGifExporter();
                var progress = new Progress<GifExportProgress>(p =>
                    Console.WriteLine($"  [{p.CompletedControls}/{p.TotalControls}] {p.Message}"));

                var result = await exporter.ExportAllAsync(
                    outputDir, progress, cts.Token, limit, only, workers, cleanExisting: useDefaultDir);

                Console.WriteLine();
                Console.WriteLine($"Done: {result.SucceededCount}/{result.TotalCount} succeeded");
                Console.WriteLine($"Output: {result.OutputDirectory}");

                if (result.Failures.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Failures:");
                    foreach (var f in result.Failures)
                        Console.WriteLine($"  - {f.ControlName}: {f.Message}");
                    exitCode = 1;
                }
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("Timeout: export took too long");
                exitCode = 3;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                exitCode = 2;
            }
            finally
            {
                tcs.TrySetResult(exitCode);
                app.Shutdown();
            }
        };

        app.Run();
        return tcs.Task.Result;
    }

    /// <summary>
    /// WinExe 不会继承调用方终端；附加父进程控制台让导出日志可见。
    /// stdout 已被重定向（如 &gt; file.txt）或没有控制台（双击启动）时保持原样。
    /// </summary>
    private static void TryAttachParentConsole()
    {
        try
        {
            if (Console.OpenStandardOutput() == Stream.Null)
            {
                AttachConsole(AttachParentProcessId);
            }
        }
        catch (IOException)
        {
            // 无控制台可用时静默跳过，导出流程不受影响
        }
    }

    private const uint AttachParentProcessId = 0xFFFFFFFF;

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(uint dwProcessId);
}
