using System.Collections.ObjectModel;
using CheemsControl.App.Infrastructure;

namespace CheemsControl.App.ViewModels;

/// <summary>
/// 导航壳 VM（规矩 M2）：左侧菜单集合 + 当前选中项，右侧内容按 PageViewModel 类型经 DataTemplate 呈现。
/// </summary>
public class MainViewModel : ObservableObject
{
    public ObservableCollection<ControlGroupViewModel> Groups { get; }

    private ControlGroupViewModel? _selectedGroup;

    public ControlGroupViewModel? SelectedGroup
    {
        get => _selectedGroup;
        set => SetProperty(ref _selectedGroup, value);
    }

    public MainViewModel()
    {
        Groups = new ObservableCollection<ControlGroupViewModel>
        {
            new("Buttons 按钮", new ButtonsViewModel()),
            new("Loaders 加载", new LoadersViewModel()),
            new("Inputs 输入", new InputsViewModel()),
        };
        _selectedGroup = Groups[0];
    }
}
