using System.Windows.Input;
using CheemsControl.App.Infrastructure;

namespace CheemsControl.App.ViewModels;

/// <summary>
/// 页面 ViewModel 的通用基类：提供轻量提示和可复用的事件演示命令。
/// </summary>
public abstract class BaseViewModel : ObservableObject
{
    private const int NotificationDurationMilliseconds = 3000;
    private string _notificationMessage = string.Empty;
    private bool _isNotificationVisible;
    private int _notificationVersion;

    protected BaseViewModel()
    {
        TestEventCommand = new RelayCommand(_ => ShowNotification("已触发测试事件"));
    }

    /// <summary>供视图底部提示层显示的文本。</summary>
    public string NotificationMessage
    {
        get => _notificationMessage;
        private set => SetProperty(ref _notificationMessage, value);
    }

    /// <summary>指示底部提示层是否可见。</summary>
    public bool IsNotificationVisible
    {
        get => _isNotificationVisible;
        private set => SetProperty(ref _isNotificationVisible, value);
    }

    /// <summary>用于演示控件命令/事件绑定的通用测试命令。</summary>
    public ICommand TestEventCommand { get; }

    /// <summary>在屏幕下方显示指定提示，默认停留三秒。</summary>
    protected void ShowNotification(string message)
    {
        var version = ++_notificationVersion;
        NotificationMessage = message;
        IsNotificationVisible = true;
        _ = HideNotificationAfterDelayAsync(version);
    }

    private async Task HideNotificationAfterDelayAsync(int version)
    {
        await Task.Delay(NotificationDurationMilliseconds);
        if (version != _notificationVersion)
        {
            return;
        }

        IsNotificationVisible = false;
    }
}
