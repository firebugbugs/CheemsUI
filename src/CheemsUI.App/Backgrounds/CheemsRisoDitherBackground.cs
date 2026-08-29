using System.IO;
using System.Globalization;
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
    private bool _isReady;
    private bool _isLoaded;

    /// <summary>用户点击预览时触发，用于将同一背景应用到窗口。</summary>
    public event EventHandler? ApplyRequested;

    public static readonly DependencyProperty ClipRadiusProperty = DependencyProperty.Register(
        nameof(ClipRadius), typeof(double), typeof(CheemsRisoDitherBackground),
        new PropertyMetadata(0d, OnClipRadiusChanged));

    public static readonly DependencyProperty PrimaryColorProperty = DependencyProperty.Register(
        nameof(PrimaryColor), typeof(Color), typeof(CheemsRisoDitherBackground),
        new PropertyMetadata(Color.FromRgb(0x8B, 0x5C, 0xF6), OnEffectOptionChanged));

    public static readonly DependencyProperty AnimationSpeedProperty = DependencyProperty.Register(
        nameof(AnimationSpeed), typeof(double), typeof(CheemsRisoDitherBackground),
        new PropertyMetadata(1d, OnEffectOptionChanged));

    public static readonly DependencyProperty IsAnimationEnabledProperty = DependencyProperty.Register(
        nameof(IsAnimationEnabled), typeof(bool), typeof(CheemsRisoDitherBackground),
        new PropertyMetadata(true, OnEffectOptionChanged));

    public static readonly DependencyProperty BackgroundAlphaProperty = DependencyProperty.Register(
        nameof(BackgroundAlpha), typeof(double), typeof(CheemsRisoDitherBackground),
        new PropertyMetadata(1d, OnEffectOptionChanged));

    public static readonly DependencyProperty PixelSizeProperty = DependencyProperty.Register(
        nameof(PixelSize), typeof(int), typeof(CheemsRisoDitherBackground),
        new PropertyMetadata(4, OnEffectOptionChanged));

    public static readonly DependencyProperty LevelsProperty = DependencyProperty.Register(
        nameof(Levels), typeof(int), typeof(CheemsRisoDitherBackground),
        new PropertyMetadata(6, OnEffectOptionChanged));

    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
        nameof(Scale), typeof(double), typeof(CheemsRisoDitherBackground),
        new PropertyMetadata(1.5d, OnEffectOptionChanged));

    public static readonly DependencyProperty ContrastProperty = DependencyProperty.Register(
        nameof(Contrast), typeof(double), typeof(CheemsRisoDitherBackground),
        new PropertyMetadata(1.2d, OnEffectOptionChanged));

    public static readonly DependencyProperty FlowAngleProperty = DependencyProperty.Register(
        nameof(FlowAngle), typeof(double), typeof(CheemsRisoDitherBackground),
        new PropertyMetadata(30d, OnEffectOptionChanged));

    public static readonly DependencyProperty DetailProperty = DependencyProperty.Register(
        nameof(Detail), typeof(double), typeof(CheemsRisoDitherBackground),
        new PropertyMetadata(0.4d, OnEffectOptionChanged));

    public static readonly DependencyProperty GlowProperty = DependencyProperty.Register(
        nameof(Glow), typeof(double), typeof(CheemsRisoDitherBackground),
        new PropertyMetadata(0.5d, OnEffectOptionChanged));

    public CheemsRisoDitherBackground()
    {
        DefaultBackgroundColor = System.Drawing.Color.Transparent;
        Cursor = Cursors.Hand;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsVisibleChanged += OnIsVisibleChanged;
        SizeChanged += (_, _) => UpdateClip();
    }

    public double ClipRadius
    {
        get => (double)GetValue(ClipRadiusProperty);
        set => SetValue(ClipRadiusProperty, value);
    }

    public Color PrimaryColor { get => (Color)GetValue(PrimaryColorProperty); set => SetValue(PrimaryColorProperty, value); }

    public double AnimationSpeed { get => (double)GetValue(AnimationSpeedProperty); set => SetValue(AnimationSpeedProperty, value); }

    public bool IsAnimationEnabled { get => (bool)GetValue(IsAnimationEnabledProperty); set => SetValue(IsAnimationEnabledProperty, value); }

    public double BackgroundAlpha { get => (double)GetValue(BackgroundAlphaProperty); set => SetValue(BackgroundAlphaProperty, value); }

    public int PixelSize { get => (int)GetValue(PixelSizeProperty); set => SetValue(PixelSizeProperty, value); }

    public int Levels { get => (int)GetValue(LevelsProperty); set => SetValue(LevelsProperty, value); }

    public double Scale { get => (double)GetValue(ScaleProperty); set => SetValue(ScaleProperty, value); }

    public double Contrast { get => (double)GetValue(ContrastProperty); set => SetValue(ContrastProperty, value); }

    public double FlowAngle { get => (double)GetValue(FlowAngleProperty); set => SetValue(FlowAngleProperty, value); }

    public double Detail { get => (double)GetValue(DetailProperty); set => SetValue(DetailProperty, value); }

    public double Glow { get => (double)GetValue(GlowProperty); set => SetValue(GlowProperty, value); }

    private Uri Page => new($"https://{VirtualHostName}/riso-dither.offline.html");

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        try
        {
            _initializationTask ??= InitializeAsync();
            await _initializationTask;
            if (IsVisible) NavigateToPage();
        }
        catch (Exception exception)
        {
            ErrorLog.Write(exception);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        _isReady = false;
        CoreWebView2?.NavigateToString("<!doctype html><title>Background paused</title>");
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            NavigateToPage();
        }
        else
        {
            _isReady = false;
            CoreWebView2?.NavigateToString("<!doctype html><title>Background paused</title>");
        }
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
        CoreWebView2.NavigationCompleted += OnNavigationCompleted;
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

    private static void OnEffectOptionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        _ = ((CheemsRisoDitherBackground)dependencyObject).PushSettingsAsync();
    }

    private async Task PushSettingsAsync()
    {
        if (CoreWebView2 is null || !_isReady) return;
        var color = $"#{PrimaryColor.R:X2}{PrimaryColor.G:X2}{PrimaryColor.B:X2}";
        var speed = AnimationSpeed.ToString("F3", CultureInfo.InvariantCulture);
        var backgroundAlpha = BackgroundAlpha.ToString("F3", CultureInfo.InvariantCulture);
        var scale = Scale.ToString("F3", CultureInfo.InvariantCulture);
        var contrast = Contrast.ToString("F3", CultureInfo.InvariantCulture);
        var flowAngle = FlowAngle.ToString("F3", CultureInfo.InvariantCulture);
        var detail = Detail.ToString("F3", CultureInfo.InvariantCulture);
        var glow = Glow.ToString("F3", CultureInfo.InvariantCulture);
        var enabled = IsAnimationEnabled ? "true" : "false";
        try
        {
            await CoreWebView2.ExecuteScriptAsync(
                $"window.__cheemsUpdate?.({{color:'{color}',backgroundAlpha:{backgroundAlpha},speed:{speed}," +
                $"pixelSize:{PixelSize},levels:{Levels},scale:{scale},contrast:{contrast},flowAngle:{flowAngle}," +
                $"detail:{detail},glow:{glow},enabled:{enabled}}});");
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void NavigateToPage()
    {
        _isReady = false;
        if (_isLoaded && CoreWebView2 is not null && CoreWebView2.Source != Page.AbsoluteUri)
        {
            CoreWebView2.Navigate(Page.AbsoluteUri);
        }
    }

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess || CoreWebView2?.Source != Page.AbsoluteUri) return;
        if (!IsVisible)
        {
            CoreWebView2.NavigateToString("<!doctype html><title>Background paused</title>");
            return;
        }
        var result = await CoreWebView2.ExecuteScriptAsync("Boolean(document.querySelector('#riso-dither-background canvas') && window.__cheemsUpdate)");
        _isReady = string.Equals(result, "true", StringComparison.Ordinal);
        if (_isReady) await PushSettingsAsync();
    }

    private void UpdateClip()
    {
        Clip = ClipRadius > 0 && ActualWidth > 0 && ActualHeight > 0
            ? new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight), ClipRadius, ClipRadius)
            : null;
    }
}
