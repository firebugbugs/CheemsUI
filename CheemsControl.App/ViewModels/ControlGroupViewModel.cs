namespace CheemsControl.App.ViewModels;

/// <summary>
/// 左侧菜单项（规矩 M9）：一个控件类型一个实例。
/// </summary>
public class ControlGroupViewModel
{
    public string Title { get; }

    /// <summary>该类型对应页面的 ViewModel，由 MainWindow 里的 DataTemplate 映射为视图。</summary>
    public object PageViewModel { get; }

    public ControlGroupViewModel(string title, object pageViewModel)
    {
        Title = title;
        PageViewModel = pageViewModel;
    }
}
