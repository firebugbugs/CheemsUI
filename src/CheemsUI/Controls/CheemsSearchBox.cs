using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CheemsUI;

/// <summary>Uiverse Li-Deheng animated search input 的 WPF 等价实现。</summary>
[TemplatePart(Name = PartInputName, Type = typeof(TextBox))]
[TemplatePart(Name = PartSearchButtonName, Type = typeof(Button))]
[TemplatePart(Name = PartClearButtonName, Type = typeof(Button))]
public sealed class CheemsSearchBox : Control
{
    private const string PartInputName = "PartInput";
    private const string PartSearchButtonName = "PartSearchButton";
    private const string PartClearButtonName = "PartClearButton";
    private const double MainDurationSeconds = 0.15;
    private const double SearchTravel = 186.9;

    private TextBox? _input;
    private Button? _searchButton;
    private Button? _clearButton;
    private FrameworkElement? _activeBorder;
    private FrameworkElement? _normalLabel;
    private FrameworkElement? _activeLabel;
    private FrameworkElement? _labelMotion;
    private FrameworkElement? _searchMotion;
    private FrameworkElement? _clearMotion;
    private ScaleTransform? _labelScale;
    private TranslateTransform? _labelTranslation;
    private TranslateTransform? _searchTranslation;
    private ScaleTransform? _clearScale;
    private RotateTransform? _clearRotation;
    private double _mainProgress;
    private double _searchProgress;
    private double _mainStart;
    private double _searchStart;
    private double _target;
    private long _transitionStartedAt;
    private bool _isActive;
    private bool _renderingSubscribed;

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(CheemsSearchBox),
        new FrameworkPropertyMetadata(
            string.Empty,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnTextChanged));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(CheemsSearchBox),
        new PropertyMetadata("Search"));

    public static readonly DependencyProperty SearchCommandProperty = DependencyProperty.Register(
        nameof(SearchCommand),
        typeof(ICommand),
        typeof(CheemsSearchBox));

    public static readonly DependencyProperty SearchCommandParameterProperty = DependencyProperty.Register(
        nameof(SearchCommandParameter),
        typeof(object),
        typeof(CheemsSearchBox));

    public static readonly RoutedEvent SearchRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(SearchRequested),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(CheemsSearchBox));

    static CheemsSearchBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsSearchBox),
            new FrameworkPropertyMetadata(typeof(CheemsSearchBox)));
    }

    public CheemsSearchBox()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public ICommand? SearchCommand
    {
        get => (ICommand?)GetValue(SearchCommandProperty);
        set => SetValue(SearchCommandProperty, value);
    }

    public object? SearchCommandParameter
    {
        get => GetValue(SearchCommandParameterProperty);
        set => SetValue(SearchCommandParameterProperty, value);
    }

    public event RoutedEventHandler SearchRequested
    {
        add => AddHandler(SearchRequestedEvent, value);
        remove => RemoveHandler(SearchRequestedEvent, value);
    }

    public override void OnApplyTemplate()
    {
        DetachTemplateHandlers();
        base.OnApplyTemplate();

        _input = GetTemplateChild(PartInputName) as TextBox;
        _searchButton = GetTemplateChild(PartSearchButtonName) as Button;
        _clearButton = GetTemplateChild(PartClearButtonName) as Button;
        _activeBorder = GetTemplateChild("PartActiveBorder") as FrameworkElement;
        _normalLabel = GetTemplateChild("PartNormalLabel") as FrameworkElement;
        _activeLabel = GetTemplateChild("PartActiveLabel") as FrameworkElement;
        _labelMotion = GetTemplateChild("PartLabelMotion") as FrameworkElement;
        _searchMotion = GetTemplateChild("PartSearchMotion") as FrameworkElement;
        _clearMotion = GetTemplateChild("PartClearMotion") as FrameworkElement;

        if (_labelMotion is not null)
        {
            _labelScale = new ScaleTransform();
            _labelTranslation = new TranslateTransform();
            var transforms = new TransformGroup();
            transforms.Children.Add(_labelScale);
            transforms.Children.Add(_labelTranslation);
            _labelMotion.RenderTransform = transforms;
        }

        if (_searchMotion is not null)
        {
            _searchTranslation = new TranslateTransform();
            _searchMotion.RenderTransform = _searchTranslation;
        }

        if (_clearMotion is not null)
        {
            _clearScale = new ScaleTransform();
            _clearRotation = new RotateTransform();
            var transforms = new TransformGroup();
            transforms.Children.Add(_clearScale);
            transforms.Children.Add(_clearRotation);
            _clearMotion.RenderTransform = transforms;
        }

        AttachTemplateHandlers();
        _isActive = (_input?.IsKeyboardFocused ?? false) || !string.IsNullOrEmpty(Text);
        _mainProgress = _isActive ? 1 : 0;
        _searchProgress = _mainProgress;
        ApplyVisuals();
    }

    private static void OnTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        ((CheemsSearchBox)dependencyObject).UpdateActiveState();
    }

    private void AttachTemplateHandlers()
    {
        if (_input is not null)
        {
            _input.GotKeyboardFocus += OnInputFocusChanged;
            _input.LostKeyboardFocus += OnInputFocusChanged;
        }

        if (_searchButton is not null)
        {
            _searchButton.Click += OnSearchClick;
        }

        if (_clearButton is not null)
        {
            _clearButton.Click += OnClearClick;
        }
    }

    private void DetachTemplateHandlers()
    {
        if (_input is not null)
        {
            _input.GotKeyboardFocus -= OnInputFocusChanged;
            _input.LostKeyboardFocus -= OnInputFocusChanged;
        }

        if (_searchButton is not null)
        {
            _searchButton.Click -= OnSearchClick;
        }

        if (_clearButton is not null)
        {
            _clearButton.Click -= OnClearClick;
        }
    }

    private void OnInputFocusChanged(object sender, KeyboardFocusChangedEventArgs e) => UpdateActiveState();

    private void OnSearchClick(object sender, RoutedEventArgs e)
    {
        var parameter = SearchCommandParameter ?? Text;
        if (SearchCommand?.CanExecute(parameter) == true)
        {
            SearchCommand.Execute(parameter);
        }

        RaiseEvent(new RoutedEventArgs(SearchRequestedEvent, this));
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        SetCurrentValue(TextProperty, string.Empty);
    }

    private void UpdateActiveState()
    {
        var active = (_input?.IsKeyboardFocused ?? false) || !string.IsNullOrEmpty(Text);
        if (active == _isActive)
        {
            return;
        }

        BeginTransition(active);
    }

    private void BeginTransition(bool active)
    {
        var now = Stopwatch.GetTimestamp();
        UpdateProgress(now);

        _isActive = active;
        _mainStart = _mainProgress;
        _searchStart = _searchProgress;
        _target = active ? 1 : 0;
        _transitionStartedAt = now;

        if (!SystemParameters.ClientAreaAnimation)
        {
            _mainProgress = _target;
            _searchProgress = _target;
            ApplyVisuals();
            UnsubscribeRendering();
            return;
        }

        SubscribeRendering();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateActiveState();
        ApplyVisuals();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => UnsubscribeRendering();

    private void OnRendering(object? sender, EventArgs e)
    {
        var complete = UpdateProgress(Stopwatch.GetTimestamp());
        ApplyVisuals();
        if (complete)
        {
            UnsubscribeRendering();
        }
    }

    private bool UpdateProgress(long now)
    {
        if (_transitionStartedAt == 0)
        {
            return true;
        }

        var elapsed = (now - _transitionStartedAt) / (double)Stopwatch.Frequency;
        _mainProgress = Advance(_mainStart, _target, elapsed, 0, MainDurationSeconds);

        _searchProgress = Advance(_searchStart, _target, elapsed, 0, MainDurationSeconds);

        return elapsed >= MainDurationSeconds;
    }

    private void ApplyVisuals()
    {
        if (_activeBorder is not null) _activeBorder.Opacity = _mainProgress;
        if (_normalLabel is not null) _normalLabel.Opacity = 1 - _mainProgress;
        if (_activeLabel is not null) _activeLabel.Opacity = _mainProgress;

        if (_labelScale is not null)
        {
            _labelScale.ScaleX = Lerp(1, 0.8, _mainProgress);
            _labelScale.ScaleY = Lerp(1, 0.8, _mainProgress);
        }

        if (_labelTranslation is not null)
        {
            _labelTranslation.Y = Lerp(16, -12.7, _mainProgress);
        }

        if (_searchTranslation is not null) _searchTranslation.X = SearchTravel * _searchProgress;
        if (_searchMotion is not null)
        {
            _searchMotion.Opacity = _searchProgress;
            _searchMotion.IsHitTestVisible = _isActive;
        }

        if (_clearScale is not null)
        {
            _clearScale.ScaleX = Lerp(0.1, 1, _mainProgress);
            _clearScale.ScaleY = Lerp(0.1, 1, _mainProgress);
        }

        if (_clearRotation is not null) _clearRotation.Angle = Lerp(-90, 0, _mainProgress);
        if (_clearMotion is not null)
        {
            _clearMotion.Opacity = _mainProgress;
            _clearMotion.IsHitTestVisible = _isActive;
        }
    }

    private static double Advance(double start, double target, double elapsed, double delay, double duration)
    {
        if (elapsed <= delay) return start;
        var linear = Math.Clamp((elapsed - delay) / duration, 0, 1);
        return Lerp(start, target, MaterialEase(linear));
    }

    // cubic-bezier(0.4, 0, 0.2, 1): solve x(t)=time, then evaluate y(t).
    private static double MaterialEase(double x)
    {
        var lower = 0.0;
        var upper = 1.0;
        var t = x;
        for (var index = 0; index < 12; index++)
        {
            t = (lower + upper) / 2;
            if (Cubic(t, 0.4, 0.2) < x) lower = t;
            else upper = t;
        }

        return Cubic(t, 0, 1);
    }

    private static double Cubic(double t, double first, double second)
    {
        var inverse = 1 - t;
        return 3 * inverse * inverse * t * first + 3 * inverse * t * t * second + t * t * t;
    }

    private static double Lerp(double from, double to, double progress) => from + ((to - from) * progress);

    private void SubscribeRendering()
    {
        if (_renderingSubscribed) return;
        CompositionTarget.Rendering += OnRendering;
        _renderingSubscribed = true;
    }

    private void UnsubscribeRendering()
    {
        if (!_renderingSubscribed) return;
        CompositionTarget.Rendering -= OnRendering;
        _renderingSubscribed = false;
    }
}
