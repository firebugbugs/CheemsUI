using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CheemsControl.App.Infrastructure;

namespace CheemsControl.App.ViewModels;

/// <summary>
/// 导航壳 VM（规矩 M2）：左侧菜单集合 + 当前选中项，右侧内容按 PageViewModel 类型经 DataTemplate 呈现。
/// </summary>
public class MainViewModel : ObservableObject
{
    public ObservableCollection<ControlGroupViewModel> Groups { get; }

    public ICollectionView GroupsView { get; }

    private ControlGroupViewModel? _selectedGroup;
    private string _searchText = string.Empty;

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

    public MainViewModel()
    {
        Groups = new ObservableCollection<ControlGroupViewModel>
        {
            new("Welcome 欢迎", new WelcomeViewModel(), "首页 home start introduction 介绍"),
            new("Buttons 按钮", new ButtonsViewModel(), "button controls 按键"),
            new("Loaders 加载", new LoadersViewModel(), "loader loading animation 动画 等待"),
            new("Inputs 输入", new InputsViewModel(), "input controls 输入控件"),
            new("Progress 进度", new ProgressViewModel(), "progress bar loading percentage 进度 进度条 百分比"),
        };
        GroupsView = CollectionViewSource.GetDefaultView(Groups);
        GroupsView.Filter = item => item is ControlGroupViewModel group && group.IsSearchMatch;
        _selectedGroup = Groups[0];
    }

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
}
