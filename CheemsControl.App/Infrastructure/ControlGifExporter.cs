using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace CheemsControl.App.Infrastructure;

internal sealed record GifExportProgress(
    double Percent,
    int CompletedControls,
    int TotalControls,
    string ControlName,
    string Message);

internal sealed record GifExportFailure(string ControlName, string Message);

internal sealed record GifExportResult(
    string OutputDirectory,
    int SucceededCount,
    int TotalCount,
    bool IsCancelled,
    IReadOnlyList<GifExportFailure> Failures);

/// <summary>逐个录制所有公开 CheemsControl 控件，并将单个失败隔离到导出报告。</summary>
internal sealed class ControlGifExporter
{
    public const int DefaultFramesPerSecond = 12;

    public static string CreateOutputDirectory()
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var root = string.IsNullOrWhiteSpace(pictures)
            ? AppDomain.CurrentDomain.BaseDirectory
            : pictures;
        return Path.Combine(
            root,
            "CheemsControl",
            "GifExports",
            DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
    }

    public async Task<GifExportResult> ExportAllAsync(
        string outputDirectory,
        IProgress<GifExportProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        Directory.CreateDirectory(outputDirectory);

        var profiles = GifRecordingProfileCatalog.CreateAll();
        var failures = new List<GifExportFailure>();
        var succeeded = 0;
        var cancelled = false;

        for (var index = 0; index < profiles.Count; index++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }

            var profile = profiles[index];
            try
            {
                progress?.Report(new GifExportProgress(
                    index * 100d / profiles.Count,
                    index,
                    profiles.Count,
                    profile.ControlType.Name,
                    $"正在准备 {profile.ControlType.Name}"));

                var frames = await CaptureControlAsync(
                    profile,
                    index,
                    profiles.Count,
                    progress,
                    cancellationToken);

                var categoryDirectory = Path.Combine(outputDirectory, profile.Category);
                var filePath = Path.Combine(categoryDirectory, $"{profile.ControlType.Name}.gif");
                AnimatedGifEncoder.Save(filePath, frames, DefaultFramesPerSecond);
                succeeded++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }
            catch (Exception exception)
            {
                failures.Add(new GifExportFailure(profile.ControlType.Name, exception.Message));
                ErrorLog.Write(new InvalidOperationException($"录制 {profile.ControlType.Name} 失败。", exception));
            }

            progress?.Report(new GifExportProgress(
                (index + 1) * 100d / profiles.Count,
                index + 1,
                profiles.Count,
                profile.ControlType.Name,
                $"已完成 {index + 1}/{profiles.Count}"));
        }

        WriteSummary(outputDirectory, profiles.Count, succeeded, cancelled, failures);
        return new GifExportResult(outputDirectory, succeeded, profiles.Count, cancelled, failures);
    }

    private static async Task<IReadOnlyList<BitmapSource>> CaptureControlAsync(
        GifRecordingProfile profile,
        int controlIndex,
        int totalControls,
        IProgress<GifExportProgress>? progress,
        CancellationToken cancellationToken)
    {
        var control = profile.CreateControl();
        var script = profile.CreateScript(control);
        using var host = new GifCaptureHost(control);
        await host.OpenAsync(cancellationToken);
        script.Start();

        if (profile.Warmup > TimeSpan.Zero)
        {
            progress?.Report(new GifExportProgress(
                controlIndex * 100d / totalControls,
                controlIndex,
                totalControls,
                profile.ControlType.Name,
                $"正在预热 {profile.ControlType.Name}"));
            await DelayWithCancellationAsync(profile.Warmup, cancellationToken);
        }

        var frameCount = Math.Max(
            1,
            (int)Math.Ceiling(profile.Duration.TotalSeconds * DefaultFramesPerSecond));
        var frames = new List<BitmapSource>(frameCount);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var target = TimeSpan.FromSeconds(frameIndex / (double)DefaultFramesPerSecond);
                var remaining = target - stopwatch.Elapsed;
                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining, cancellationToken);
                }

                script.Update(target);
                await host.PrepareFrameAsync(cancellationToken);
                frames.Add(host.Capture());

                var withinControl = (frameIndex + 1d) / frameCount;
                progress?.Report(new GifExportProgress(
                    (controlIndex + withinControl) * 100d / totalControls,
                    controlIndex,
                    totalControls,
                    profile.ControlType.Name,
                    $"正在录制 {profile.ControlType.Name} · {frameIndex + 1}/{frameCount} 帧"));
            }
        }
        finally
        {
            script.Finish();
        }

        return frames;
    }

    private static Task DelayWithCancellationAsync(TimeSpan duration, CancellationToken cancellationToken) =>
        duration <= TimeSpan.Zero ? Task.CompletedTask : Task.Delay(duration, cancellationToken);

    private static void WriteSummary(
        string outputDirectory,
        int total,
        int succeeded,
        bool cancelled,
        IReadOnlyCollection<GifExportFailure> failures)
    {
        var text = new StringBuilder()
            .AppendLine("CheemsControl GIF Export")
            .AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            .AppendLine($"Frame rate: {DefaultFramesPerSecond} FPS")
            .AppendLine("Background: transparent")
            .AppendLine($"Result: {succeeded}/{total} succeeded")
            .AppendLine($"Cancelled: {cancelled}");

        if (failures.Count > 0)
        {
            text.AppendLine().AppendLine("Failures:");
            foreach (var failure in failures)
            {
                text.AppendLine($"- {failure.ControlName}: {failure.Message}");
            }
        }

        File.WriteAllText(Path.Combine(outputDirectory, "export-summary.txt"), text.ToString());
    }
}
