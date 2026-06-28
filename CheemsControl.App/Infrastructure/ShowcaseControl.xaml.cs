using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace CheemsControl.App.Infrastructure;

/// <summary>
/// 复杂控件展示单元：提供标题、展示台、源码预览与复制。
/// Title = 示例名；Demo = 被演示控件；Source = 源码资源 pack URI。
/// 交互：hover「&lt;/&gt;」预览源码，点击复制并短暂反馈。
/// </summary>
public partial class ShowcaseControl : UserControl
{
    private const string CodeIcon = "</>";
    private const string CopyingText = "复制中…";
    private const string CopiedText = "已复制 ✓";
    private const string CopyFailedText = "复制失败 ✗";

    private bool _sourceLoaded;
    private int _copyToken;
    private int _hoverToken;

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(ShowcaseControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DemoProperty = DependencyProperty.Register(
        nameof(Demo), typeof(object), typeof(ShowcaseControl), new PropertyMetadata(null));

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(string), typeof(ShowcaseControl), new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object Demo
    {
        get => GetValue(DemoProperty);
        set => SetValue(DemoProperty, value);
    }

    /// <summary>源码资源 pack 相对 URI，对应 Sources/ 下的 .xaml.txt（规矩 M4）。</summary>
    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public ShowcaseControl()
    {
        InitializeComponent();
    }

    private void CodeButton_MouseEnter(object sender, MouseEventArgs e)
    {
        _hoverToken++;
        EnsureSourceLoaded();
        CodePopup.IsOpen = true;
    }

    private async void CodeButton_MouseLeave(object sender, MouseEventArgs e)
    {
        var token = ++_hoverToken;
        await Task.Delay(80);

        if (token == _hoverToken && !IsPointerInsideCodeButton())
        {
            CodePopup.IsOpen = false;
        }
    }

    private bool IsPointerInsideCodeButton()
    {
        var position = Mouse.GetPosition(CodeButton);
        return position.X >= 0 && position.Y >= 0
            && position.X <= CodeButton.ActualWidth
            && position.Y <= CodeButton.ActualHeight;
    }

    private async void CodeButton_Click(object sender, RoutedEventArgs e)
    {
        EnsureSourceLoaded();
        var token = ++_copyToken;
        CodeButton.IsEnabled = false;
        CodeButton.Content = CopyingText;

        var window = Window.GetWindow(this);
        var ownerHandle = window is null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;
        var succeeded = await SourceCodeService.TryCopyToClipboardAsync(CodeText.Text, ownerHandle);

        CodeButton.IsEnabled = true;
        CodeButton.Content = succeeded ? CopiedText : CopyFailedText;
        await Task.Delay(1200);
        if (token == _copyToken)
        {
            CodeButton.Content = CodeIcon;
        }
    }

    private void EnsureSourceLoaded()
    {
        if (_sourceLoaded || string.IsNullOrEmpty(Source))
        {
            return;
        }

        CodeText.Text = SourceCodeService.Load(Source);
        _sourceLoaded = true;
    }
}
