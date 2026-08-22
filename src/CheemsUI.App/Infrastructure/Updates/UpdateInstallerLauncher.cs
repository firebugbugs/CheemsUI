using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace CheemsUI.App.Infrastructure.Updates;

internal static class UpdateInstallerLauncher
{
    private const string InstallArgument = "--install-update";

    public static void LaunchInstallerAfterExit(string installerPath)
    {
        var processPath = Environment.ProcessPath ?? throw new InvalidOperationException("无法确定当前程序路径。");
        var currentProcessId = Environment.ProcessId;
        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.FileName = processPath;
            startInfo.ArgumentList.Add(Assembly.GetEntryAssembly()?.Location ?? throw new InvalidOperationException("无法确定应用程序集路径。"));
        }
        else
        {
            startInfo.FileName = processPath;
        }

        startInfo.ArgumentList.Add(InstallArgument);
        startInfo.ArgumentList.Add(installerPath);
        startInfo.ArgumentList.Add(currentProcessId.ToString());
        _ = Process.Start(startInfo) ?? throw new InvalidOperationException("无法启动更新安装程序。");
    }

    public static bool TryRunInstaller(string[] args, out int exitCode)
    {
        exitCode = 0;
        if (args.Length != 3 || !string.Equals(args[0], InstallArgument, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!File.Exists(args[1]))
        {
            exitCode = 2;
            return true;
        }

        if (!int.TryParse(args[2], out var originalProcessId))
        {
            exitCode = 2;
            return true;
        }

        try
        {
            using var originalProcess = Process.GetProcessById(originalProcessId);
            if (!originalProcess.WaitForExit(30000))
            {
                exitCode = 3;
                return true;
            }
        }
        catch (ArgumentException)
        {
            // The original process has already exited, which is the expected fast path.
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = args[1],
                UseShellExecute = true
            });
        }
        catch
        {
            exitCode = 4;
        }

        return true;
    }
}
