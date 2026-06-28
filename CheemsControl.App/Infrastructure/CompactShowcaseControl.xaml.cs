using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace CheemsControl.App.Infrastructure;

/// <summary>
/// 简单控件的透明横向展示器：Demo + 源码预览/复制按钮。
/// </summary>
public partial class CompactShowcaseControl : UserControl
{
    private const string CodeIcon = "</>";
    private const string CopyingText = "复制中…";
    private const string CopiedText = "已复制 ✓";
    private const string CopyFailedText = "复制失败 ✗";

    private bool _sourceLoaded;
    private int _copyToken;

    public static readonly DependencyProperty DemoProperty = DependencyProperty.Register(
        nameof(Demo), typeof(object), typeof(CompactShowcaseControl), new PropertyMetadata(null));

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(string), typeof(CompactShowcaseControl), new PropertyMetadata(string.Empty));

    public object Demo
    {
        get => GetValue(DemoProperty);
        set => SetValue(DemoProperty, value);
    }

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public CompactShowcaseControl()
    {
        InitializeComponent();
    }

    private void CodeButton_MouseEnter(object sender, MouseEventArgs e)
    {
        EnsureSourceLoaded();
        CodePopup.IsOpen = true;
    }

    private void CodeButton_MouseLeave(object sender, MouseEventArgs e)
    {
        CodePopup.IsOpen = false;
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
