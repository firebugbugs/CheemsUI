using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CheemsUI.App.Infrastructure;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace CheemsUI.App.Backgrounds;

/// <summary>使用固定本地 WebGL 着色器的离线 Riso Dither 背景层。</summary>
public sealed class CheemsRisoDitherBackground : WebView2CompositionControl
{
    private const string VirtualHostName = "riso-dither.cheemsui.local";
    private Task? _initializationTask;

    /// <summary>用户点击预览时触发，用于将同一背景应用到窗口。</summary>
    public event EventHandler? ApplyRequested;

    public static readonly DependencyProperty ClipRadiusProperty = DependencyProperty.Register(
        nameof(ClipRadius), typeof(double), typeof(CheemsRisoDitherBackground),
        new PropertyMetadata(0d, OnClipRadiusChanged));

    public CheemsRisoDitherBackground()
    {
        Cursor = Cursors.Hand;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += (_, _) => UpdateClip();
    }

    public double ClipRadius
    {
        get => (double)GetValue(ClipRadiusProperty);
        set => SetValue(ClipRadiusProperty, value);
    }

    private Uri Page => new($"https://{VirtualHostName}/riso-dither.offline.html");

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _initializationTask ??= InitializeAsync();
            await _initializationTask;
            if (CoreWebView2?.Source != Page.AbsoluteUri)
            {
                CoreWebView2?.Navigate(Page.AbsoluteUri);
            }
        }
        catch (Exception exception)
        {
            ErrorLog.Write(exception);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        CoreWebView2?.NavigateToString("<!doctype html><title>Background paused</title>");
    }

    private async Task InitializeAsync()
    {
        await EnsureCoreWebView2Async();
        var assetFolder = Path.Combine(AppContext.BaseDirectory, "Assets", "RisoDither");
        if (!File.Exists(Path.Combine(assetFolder, "riso-dither.offline.html")))
        {
            throw new FileNotFoundException("未找到离线 Riso Dither 资源。", assetFolder);
        }

        CoreWebView2.SetVirtualHostNameToFolderMapping(
            VirtualHostName, assetFolder, CoreWebView2HostResourceAccessKind.DenyCors);
        CoreWebView2.WebMessageReceived += OnWebMessageReceived;
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (e.TryGetWebMessageAsString() == "apply-background")
        {
            ApplyRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private static void OnClipRadiusChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        ((CheemsRisoDitherBackground)dependencyObject).UpdateClip();
    }

    private void UpdateClip()
    {
        Clip = ClipRadius > 0 && ActualWidth > 0 && ActualHeight > 0
            ? new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight), ClipRadius, ClipRadius)
            : null;
    }
}
