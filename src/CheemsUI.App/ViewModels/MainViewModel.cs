using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Data;
using CheemsUI.App.Infrastructure;

namespace CheemsUI.App.ViewModels;

/// <summary>
/// 导航壳 VM（规矩 M2）：左侧菜单集合 + 当前选中项，右侧内容按 PageViewModel 类型经 DataTemplate 呈现。
/// </summary>
public class MainViewModel : ObservableObject
{
    public ObservableCollection<ControlGroupViewModel> Groups { get; }

    public ICollectionView GroupsView { get; }

    private ControlGroupViewModel? _selectedGroup;
    private string _searchText = string.Empty;
    private CancellationTokenSource? _gifExportCancellation;
    private bool _isGifExporting;
    private double _gifExportProgress;
    private string _gifExportStatus = "透明背景 · 12 FPS";
    private string? _lastGifExportDirectory;

    public ControlGroupViewModel? SelectedGroup
    {
        get => _selectedGroup;
        set => SetProperty(ref _selectedGroup, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value)) return;
            ApplySearch();
        }
    }

    public bool IsGifExporting
    {
        get => _isGifExporting;
        private set
        {
            if (!SetProperty(ref _isGifExporting, value))
            {
                return;
            }

            OnPropertyChanged(nameof(GifExportActionText));
        }
    }

    public double GifExportProgress
    {
        get => _gifExportProgress;
        private set => SetProperty(ref _gifExportProgress, value);
    }

    public string GifExportStatus
    {
        get => _gifExportStatus;
        private set => SetProperty(ref _gifExportStatus, value);
    }

    public string GifExportActionText => IsGifExporting ? "取消生成" : "生成全部 GIF";

    public string? LastGifExportDirectory
    {
        get => _lastGifExportDirectory;
        private set
        {
            if (!SetProperty(ref _lastGifExportDirectory, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasGifExportDirectory));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool HasGifExportDirectory =>
        !string.IsNullOrWhiteSpace(LastGifExportDirectory) && Directory.Exists(LastGifExportDirectory);

    public ICommand ExportAllGifsCommand { get; }

    public ICommand OpenGifExportDirectoryCommand { get; }

    public MainViewModel()
    {
        ExportAllGifsCommand = new RelayCommand(_ => ToggleGifExport());
        OpenGifExportDirectoryCommand = new RelayCommand(
            _ => OpenGifExportDirectory(),
            _ => HasGifExportDirectory);

        Groups = new ObservableCollection<ControlGroupViewModel>
        {
            new("Welcome 欢迎", new WelcomeViewModel(), "首页 home start introduction 介绍"),
            new("Buttons 按钮", new ButtonsViewModel(), "button controls 按键"),
            new("Loaders 加载", new LoadersViewModel(), "loader loading animation 动画 等待"),
            new("Inputs 输入", new InputsViewModel(), "input controls 输入控件"),
            new("Progress 进度", new ProgressViewModel(), "progress bar loading percentage 进度 进度条 百分比"),
            new("Backgrounds 背景", new BackgroundsViewModel(), "background birds vanta three webgl 飞鸟 鸟群 背景 特效 离线"),
        };
        GroupsView = CollectionViewSource.GetDefaultView(Groups);
        GroupsView.Filter = item => item is ControlGroupViewModel group && group.IsSearchMatch;
        _selectedGroup = Groups[0];
    }

    public void CancelGifExport() => _gifExportCancellation?.Cancel();

    private void ApplySearch()
    {
        var query = SearchText.Trim();
        foreach (var group in Groups)
        {
            var titleMatches = string.IsNullOrEmpty(query) ||
                               SearchablePageViewModel.Matches(query, group.SearchTerms);

            if (group.PageViewModel is ISearchablePageViewModel searchablePage)
            {
                searchablePage.ApplySearch(query, titleMatches);
                group.IsSearchMatch = titleMatches || searchablePage.HasMatches;
            }
            else
            {
                group.IsSearchMatch = titleMatches;
            }
        }

        GroupsView.Refresh();
        if (SelectedGroup is null || !SelectedGroup.IsSearchMatch)
        {
            SelectedGroup = Groups.FirstOrDefault(group => group.IsSearchMatch);
        }
    }

    private void ToggleGifExport()
    {
        if (IsGifExporting)
        {
            GifExportStatus = "正在取消，当前帧完成后停止…";
            _gifExportCancellation?.Cancel();
            return;
        }

        _ = ExportAllGifsAsync();
    }

    private async Task ExportAllGifsAsync()
    {
        _gifExportCancellation?.Dispose();
        _gifExportCancellation = new CancellationTokenSource();
        var cancellationToken = _gifExportCancellation.Token;
        var outputDirectory = ControlGifExporter.GetDefaultOutputDirectory();

        IsGifExporting = true;
        GifExportProgress = 0;
        GifExportStatus = "正在扫描控件…";
        LastGifExportDirectory = outputDirectory;

        try
        {
            var exporter = new ControlGifExporter();
            var progress = new CallbackProgress<GifExportProgress>(update =>
            {
                GifExportProgress = update.Percent;
                GifExportStatus = update.Message;
            });
            var result = await exporter.ExportAllAsync(outputDirectory, progress, cancellationToken,
                cleanExisting: true);

            if (result.IsCancelled)
            {
                GifExportStatus = $"已取消 · 保留 {result.SucceededCount} 个 GIF";
            }
            else if (result.Failures.Count > 0)
            {
                GifExportProgress = 100;
                GifExportStatus = $"完成 {result.SucceededCount}/{result.TotalCount} · 失败详情见报告";
            }
            else
            {
                GifExportProgress = 100;
                GifExportStatus = $"已生成 {result.SucceededCount} 个 GIF";
            }
        }
        catch (Exception exception)
        {
            ErrorLog.Write(exception);
            GifExportStatus = $"生成失败：{exception.Message}";
        }
        finally
        {
            IsGifExporting = false;
            _gifExportCancellation?.Dispose();
            _gifExportCancellation = null;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void OpenGifExportDirectory()
    {
        if (!HasGifExportDirectory)
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = LastGifExportDirectory!,
            UseShellExecute = true
        });
    }

    private sealed class CallbackProgress<T>(Action<T> callback) : IProgress<T>
    {
        public void Report(T value) => callback(value);
    }
}
