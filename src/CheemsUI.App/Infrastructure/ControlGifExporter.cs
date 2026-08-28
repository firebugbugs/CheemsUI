using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CheemsUI.App.Infrastructure;

internal sealed record GifExportProgress(
    double Percent, int CompletedControls, int TotalControls,
    string ControlName, string Message);

internal sealed record GifExportFailure(string ControlName, string Message);

internal sealed record GifExportResult(
    string OutputDirectory, int SucceededCount, int TotalCount,
    bool IsCancelled, IReadOnlyList<GifExportFailure> Failures);

/// <summary>
/// 导出所有控件（全部为 GIF）：
/// Loader 循环动画（四周扩 10%）；
/// 按钮交互（常态 → 移入 → 按下 0.3s → 抬起停留 0.5s → 离开，带可替换的虚拟光标）；
/// 开关交互（常态 → 移入 → 点击打开 → 再点击关闭 → 移开，带光标）；
/// 输入框交互（常态 → 移入 → 点击聚焦 → 逐字输入 "Cheems"，带光标）；
/// 进度条（0 → 100% 全程 5s）。
/// 录制在多个 STA 工作线程上并行：每个线程有独立 Dispatcher/宿主窗口（按屏幕网格铺开互不遮挡），
/// GIF 编码只处理已 Freeze 的位图，放线程池执行，不占用录制线程。
/// </summary>
internal sealed class ControlGifExporter
{
    public const int DefaultFramesPerSecond = 24;
    public const int DefaultWorkerCount = 4;
    private const double LoaderExpandPercent = 10;
    /// <summary>首帧前预热：留出 Loaded → CompositionTarget.Rendering 自驱动动画完成自举的时间（高负载下首个渲染 tick 可能滞后）。</summary>
    private static readonly TimeSpan FirstFrameWarmup = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// 默认输出目录：固定为仓库 docs\gallery，README 可用稳定相对路径引用（如 docs/gallery/Loaders/X.gif）。
    /// 从 exe 所在位置向上查找 .git 定位仓库根；找不到（发布到仓库外运行）时退回 exe 同级目录。
    /// </summary>
    public static string GetDefaultOutputDirectory()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
        {
            dir = dir.Parent;
        }

        var root = dir?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(root, "docs", "gallery");
    }

    public async Task<GifExportResult> ExportAllAsync(
        string outputDirectory,
        IProgress<GifExportProgress>? progress,
        CancellationToken cancellationToken,
        int limit = 0,
        string? onlyNames = null,
        int workerCount = DefaultWorkerCount,
        bool cleanExisting = false)
    {
        Directory.CreateDirectory(outputDirectory);

        var profiles = GifRecordingProfileCatalog.CreateAll();
        if (onlyNames is not null)
        {
            var wanted = onlyNames.Split(',', ';')
                .Select(n => n.Trim())
                .Where(n => n.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            profiles = profiles.Where(p => wanted.Contains(p.ControlType.Name)).ToArray();
        }
        if (limit > 0) profiles = profiles.Take(limit).ToArray();

        // 固定图库路径重复导出时清掉旧分类目录，避免已删除/改名的控件留下过期图片误导 README
        if (cleanExisting)
        {
            foreach (var category in profiles.Select(p => p.Category).Distinct())
            {
                var categoryDirectory = Path.Combine(outputDirectory, category);
                if (Directory.Exists(categoryDirectory))
                {
                    Directory.Delete(categoryDirectory, recursive: true);
                }
            }
        }

        workerCount = Math.Clamp(workerCount, 1, Math.Min(profiles.Count, GifCaptureHost.MaxSlots));

        var counters = new ExportCounters(profiles.Count);
        var workers = new List<RecordingWorker>();
        for (var w = 0; w < workerCount; w++)
        {
            // 轮询分配让长耗时的 Loader 均摊到各工作线程
            var queue = new List<GifRecordingProfile>();
            for (var i = w; i < profiles.Count; i += workerCount)
            {
                queue.Add(profiles[i]);
            }

            workers.Add(new RecordingWorker(this, queue, w, outputDirectory, progress, cancellationToken, counters));
        }

        foreach (var worker in workers)
        {
            worker.Start();
        }

        await Task.WhenAll(workers.Select(w => w.Completion));

        // 录制线程已全部收工，等剩余的池上编码任务完成
        await Task.WhenAll(counters.TakeEncodes());

        WriteSummary(outputDirectory, counters);
        return new GifExportResult(outputDirectory, counters.Succeeded, profiles.Count,
            counters.Cancelled, counters.Failures);
    }

    private async Task ExportGifAsync(
        GifRecordingProfile profile, string categoryDirectory, Point position,
        IProgress<GifExportProgress>? progress, CancellationToken ct, ExportCounters counters)
    {
        // 第一遍空跑：采样动画全程的渲染边界并集（NewtonsCradle 等控件运动会超出初始尺寸）
        var motionBounds = await ProbeMotionBoundsAsync(profile, position, progress, ct, counters);

        var control = profile.CreateControl();
        var script = profile.CreateScript(control);
        using var host = new GifCaptureHost(control, LoaderExpandPercent, position, motionBounds);

        await host.OpenAsync(ct);
        script.Attach(host);
        script.Start();
        await DelayAsync(FirstFrameWarmup, ct);

        if (profile.Warmup > TimeSpan.Zero)
            await DelayAsync(profile.Warmup, ct);

        // GIF 的 1/100 秒延迟精度无法保证所有动画周期都是 1/FPS 的整数倍
        // （例如 OrbitDots 的 2.4s 在 24 FPS 下为 57.6 帧）。采样时间轴按完整
        // 录制时长均分，并把相同的总时长交给编码器，循环点便能与控件周期精确对齐。
        var frameCount = Math.Max(1, (int)Math.Ceiling(profile.Duration.TotalSeconds * DefaultFramesPerSecond));
        var frameInterval = TimeSpan.FromTicks(profile.Duration.Ticks / frameCount);
        var frames = new List<BitmapSource>(frameCount);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            for (var fi = 0; fi < frameCount; fi++)
            {
                ct.ThrowIfCancellationRequested();
                var target = TimeSpan.FromTicks(frameInterval.Ticks * fi);
                var remaining = target - stopwatch.Elapsed;
                if (remaining > TimeSpan.Zero)
                    await DelayAsync(remaining, ct);

                script.Update(target);
                host.PrepareFrame();
                frames.Add(host.Capture());

                progress?.Report(counters.Report(
                    profile.ControlType.Name, $"录制 {profile.ControlType.Name} · {fi + 1}/{frameCount} 帧"));
            }
        }
        finally
        {
            script.Finish();
        }

        var filePath = Path.Combine(categoryDirectory, $"{profile.ControlType.Name}.gif");

        // 虚拟光标在编码线程上按帧时间合成，不参与 WPF 渲染（避免扰动效果光栅化导致循环跳变）
        var usesCursor = profile.UsesCursorOverlay;
        var stageScale = frames.Count > 0 && host.StageSize.Width > 0
            ? frames[0].PixelWidth / host.StageSize.Width
            : 1.0;

        counters.QueueEncode(profile.ControlType.Name, Task.Run(() =>
        {
            if (usesCursor && RecordingCursor.IsAvailable)
            {
                for (var i = 0; i < frames.Count; i++)
                {
                    var time = TimeSpan.FromTicks(frameInterval.Ticks * i);
                    if (script.GetCursorPosition(time) is { } tip)
                        frames[i] = RecordingCursor.Composite(frames[i], tip, stageScale);
                }
            }

            // 光标合成完再做统一缩放，光标与控件始终保持相同的视觉比例。
            var normalizedFrames = GifFrameSizeNormalizer.NormalizeHeights(frames);
            AnimatedGifEncoder.Save(filePath, normalizedFrames, DefaultFramesPerSecond, profile.Duration);
        }));
    }

    /// <summary>
    /// 空跑一遍动画：以 ~60Hz 采样渲染边界并集，得到运动过程的最大边界。
    /// 采样密度高于录制帧率（24fps），录制帧不会超出探测结果。
    /// </summary>
    private async Task<Rect> ProbeMotionBoundsAsync(
        GifRecordingProfile profile, Point position,
        IProgress<GifExportProgress>? progress, CancellationToken ct, ExportCounters counters)
    {
        progress?.Report(counters.Report(
            profile.ControlType.Name, $"分析 {profile.ControlType.Name} 运动边界"));

        var control = profile.CreateControl();
        var script = profile.CreateScript(control);
        using var host = new GifCaptureHost(control, LoaderExpandPercent, position);

        await host.OpenAsync(ct);
        script.Start();
        await DelayAsync(FirstFrameWarmup, ct);

        if (profile.Warmup > TimeSpan.Zero)
            await DelayAsync(profile.Warmup, ct);

        var union = host.GetRenderBounds();
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < profile.Duration)
        {
            script.Update(stopwatch.Elapsed);
            await DelayAsync(TimeSpan.FromMilliseconds(16), ct);
            union.Union(host.GetRenderBounds());
        }

        script.Finish();
        return union;
    }

    /// <summary>
    /// 异步等待，期间 Dispatcher 空闲，布局、渲染与动画时钟正常推进。
    /// 不能用 Thread.Sleep：它会冻结 UI 线程，画面永远停在第一帧。
    /// </summary>
    private static async Task DelayAsync(TimeSpan duration, CancellationToken ct)
    {
        if (duration <= TimeSpan.Zero) return;
        await Task.Delay(duration, ct);
    }

    private static void WriteSummary(string outputDirectory, ExportCounters counters)
    {
        var text = new StringBuilder()
            .AppendLine("CheemsUI Export")
            .AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            .AppendLine($"Frame rate: {DefaultFramesPerSecond} FPS")
            .AppendLine("Loaders: GIF (animated, +10% padding)")
            .AppendLine("Buttons: GIF (normal -> hover -> press 0.3s -> dwell 0.5s -> leave, custom cursor)")
            .AppendLine("Toggles: GIF (normal -> hover -> click on -> click off -> leave, custom cursor)")
            .AppendLine("Inputs: GIF (normal -> hover -> click focus -> type \"Cheems\", custom cursor)")
            .AppendLine("Progress: GIF (value 0 -> 100% in 5s)")
            .AppendLine($"Result: {counters.Succeeded}/{counters.TotalControls} succeeded")
            .AppendLine($"Cancelled: {counters.Cancelled}");

        if (counters.Failures.Count > 0)
        {
            text.AppendLine().AppendLine("Failures:");
            foreach (var f in counters.Failures)
                text.AppendLine($"- {f.ControlName}: {f.Message}");
        }

        File.WriteAllText(Path.Combine(outputDirectory, "export-summary.txt"), text.ToString());
    }

    /// <summary>跨线程的进度/计数聚合。</summary>
    private sealed class ExportCounters
    {
        private readonly object _gate = new();
        private readonly List<Task> _pendingEncodes = new();

        public ExportCounters(int totalControls) => TotalControls = totalControls;

        public int TotalControls { get; }
        public int Succeeded { get; private set; }
        public int Completed { get; private set; }
        public bool Cancelled { get; private set; }
        public List<GifExportFailure> Failures { get; } = new();

        public GifExportProgress Report(string controlName, string message)
        {
            lock (_gate)
            {
                return new GifExportProgress(
                    Completed * 100d / Math.Max(1, TotalControls), Completed, TotalControls,
                    controlName, message);
            }
        }

        public void Complete(string controlName)
        {
            lock (_gate)
            {
                Succeeded++;
                Completed++;
            }
        }

        public void Fail(string controlName, string message)
        {
            lock (_gate)
            {
                Failures.Add(new GifExportFailure(controlName, message));
                Completed++;
            }
        }

        public void Cancel()
        {
            lock (_gate) Cancelled = true;
        }

        public void QueueEncode(string controlName, Task encodeTask)
        {
            lock (_gate) _pendingEncodes.Add(encodeTask);
            _ = encodeTask.ContinueWith(
                t => Fail(controlName, t.Exception!.GetBaseException().Message),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        public Task[] TakeEncodes()
        {
            lock (_gate) return _pendingEncodes.ToArray();
        }
    }

    /// <summary>
    /// 单个 STA 录制线程：独立 Dispatcher + 按屏幕网格定位的宿主窗口，
    /// 队列内的控件依次录制，GIF 编码移交线程池后立即录制下一个。
    /// </summary>
    private sealed class RecordingWorker
    {
        private readonly ControlGifExporter _owner;
        private readonly IReadOnlyList<GifRecordingProfile> _profiles;
        private readonly int _slot;
        private readonly string _outputDirectory;
        private readonly IProgress<GifExportProgress>? _progress;
        private readonly CancellationToken _ct;
        private readonly ExportCounters _counters;
        private readonly TaskCompletionSource _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public RecordingWorker(
            ControlGifExporter owner, IReadOnlyList<GifRecordingProfile> profiles, int slot,
            string outputDirectory, IProgress<GifExportProgress>? progress,
            CancellationToken ct, ExportCounters counters)
        {
            _owner = owner;
            _profiles = profiles;
            _slot = slot;
            _outputDirectory = outputDirectory;
            _progress = progress;
            _ct = ct;
            _counters = counters;
        }

        public Task Completion => _completion.Task;

        public void Start()
        {
            var thread = new Thread(Run)
            {
                IsBackground = true,
                Name = $"CheemsExport-{_slot}"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void Run()
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            // await 的续延必须回到本线程的 Dispatcher，否则会跨线程访问 UI 对象
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            _ = RunQueueAsync(dispatcher);
            Dispatcher.Run();
        }

        private async Task RunQueueAsync(Dispatcher dispatcher)
        {
            var position = GifCaptureHost.GetSlotOrigin(_slot);
            try
            {
                foreach (var profile in _profiles)
                {
                    if (_ct.IsCancellationRequested)
                    {
                        _counters.Cancel();
                        break;
                    }

                    try
                    {
                        _progress?.Report(_counters.Report(
                            profile.ControlType.Name, $"正在导出 {profile.ControlType.Name}"));

                        var categoryDirectory = Path.Combine(_outputDirectory, profile.Category);
                        Directory.CreateDirectory(categoryDirectory);

                        await _owner.ExportGifAsync(profile, categoryDirectory, position, _progress, _ct, _counters);

                        _counters.Complete(profile.ControlType.Name);
                        _progress?.Report(_counters.Report(
                            profile.ControlType.Name, $"{profile.ControlType.Name} 完成"));
                    }
                    catch (OperationCanceledException) when (_ct.IsCancellationRequested)
                    {
                        _counters.Cancel();
                        break;
                    }
                    catch (Exception ex)
                    {
                        _counters.Fail(profile.ControlType.Name, ex.InnerException?.Message ?? ex.Message);
                        ErrorLog.Write(new InvalidOperationException($"导出 {profile.ControlType.Name} 失败。", ex));
                    }
                }
            }
            catch (Exception ex)
            {
                _counters.Fail($"worker-{_slot}", ex.Message);
                ErrorLog.Write(ex);
            }
            finally
            {
                dispatcher.InvokeShutdown();
                _completion.TrySetResult();
            }
        }
    }
}
