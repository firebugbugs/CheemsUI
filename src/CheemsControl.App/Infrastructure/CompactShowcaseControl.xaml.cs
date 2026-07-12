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
    private bool _sourceLoaded;
    private bool _copyInProgress;
    private int _copyToken;
    private int _hoverToken;

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
        _hoverToken++;
        EnsureSourceLoaded();
        CodePopup.IsOpen = true;
    }

    private async void CodeButton_MouseLeave(object sender, MouseEventArgs e)
    {
        var token = ++_hoverToken;
        await Task.Delay(80);

        // Popup 是独立窗口；打开瞬间可能令按钮短暂收到 MouseLeave。
        // 延迟后再次按屏幕指针位置确认，避免在打开/关闭之间循环闪烁。
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
        if (_copyInProgress)
        {
            return;
        }

        EnsureSourceLoaded();
        var token = ++_copyToken;
        _copyInProgress = true;
        CodeButton.Tag = "Copying";

        var window = Window.GetWindow(this);
        var ownerHandle = window is null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;
        var succeeded = await SourceCodeService.TryCopyToClipboardAsync(CodeText.Text, ownerHandle);

        _copyInProgress = false;
        CodeButton.Tag = succeeded ? "Success" : "Failure";
        await Task.Delay(1200);
        if (token == _copyToken)
        {
            CodeButton.Tag = null;
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
