using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using CheemsControl;

namespace CheemsControl.App.Infrastructure;

internal sealed class GifRecordingProfile
{
    private readonly Action<Control> _configure;
    private readonly Func<Control, ControlRecordingScript> _scriptFactory;

    public GifRecordingProfile(
        Type controlType,
        string category,
        TimeSpan duration,
        TimeSpan warmup,
        Action<Control> configure,
        Func<Control, ControlRecordingScript> scriptFactory)
    {
        ControlType = controlType;
        Category = category;
        Duration = duration;
        Warmup = warmup;
        _configure = configure;
        _scriptFactory = scriptFactory;
    }

    public Type ControlType { get; }

    public string Category { get; }

    public TimeSpan Duration { get; }

    public TimeSpan Warmup { get; }

    public Control CreateControl()
    {
        var control = Activator.CreateInstance(ControlType) as Control
                      ?? throw new InvalidOperationException($"无法创建控件 {ControlType.FullName}。");
        control.UseLayoutRounding = true;
        _configure(control);
        return control;
    }

    public ControlRecordingScript CreateScript(Control control) => _scriptFactory(control);
}

/// <summary>
/// 自动发现类库中的公开控件，并按继承类型生成默认录制脚本。
/// 特殊动画只登记周期/预热覆盖项，新增控件不会因遗漏清单而完全不导出。
/// </summary>
internal static class GifRecordingProfileCatalog
{
    private static readonly IReadOnlyDictionary<string, (double Duration, double Warmup)> LoaderTimings =
        new Dictionary<string, (double, double)>(StringComparer.Ordinal)
        {
            [nameof(CheemsAiMatrixLoader)] = (2.0, 1.5),
            [nameof(CheemsBlobLoader)] = (2.0, 0),
            [nameof(CheemsBounceBallLoader)] = (1.0, 0),
            [nameof(CheemsCubeLoadingLoader)] = (2.1, 1.2),
            [nameof(CheemsDominoLoader)] = (1.0, 0.865),
            [nameof(CheemsEarthLoader)] = (5.0, 0.75),
            [nameof(CheemsGlitchLoader)] = (2.0, 0),
            [nameof(CheemsHamsterWheelLoader)] = (1.0, 0),
            [nameof(CheemsJumpingSquareLoader)] = (0.5, 0),
            [nameof(CheemsNewtonsCradleLoader)] = (1.2, 0),
            [nameof(CheemsOrbitDotsLoader)] = (2.4, 0),
            [nameof(CheemsPolylineLoader)] = (1.4, 0),
            [nameof(CheemsPulseDotsLoader)] = (1.5, 0.1),
            [nameof(CheemsRainbowBarsLoader)] = (0.45, 0.4),
            [nameof(CheemsTypewriterLoader)] = (3.0, 0),
            [nameof(CheemsWashingMachineLoader)] = (3.0, 0),
            [nameof(CheemsWaveBarsLoader)] = (3.0, 0.1)
        };

    private static readonly IReadOnlyDictionary<string, object?> InitialContent =
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(CheemsDashedButton)] = "BUTTON",
            [nameof(CheemsSoftButton)] = "Click me",
            [nameof(CheemsShineButton)] = "SHINE",
            [nameof(CheemsDeleteButton)] = null,
            [nameof(CheemsSubscribeButton)] = "Subscribe",
            [nameof(CheemsLayered3DButton)] = "Button",
            [nameof(CheemsPixelHandButton)] = null,
            [nameof(CheemsLeafButton)] = "Button",
            [nameof(CheemsSaharaButton)] = "WELCOME"
        };

    public static IReadOnlyList<GifRecordingProfile> CreateAll()
    {
        return typeof(CheemsDashedButton).Assembly
            .GetTypes()
            .Where(type => type.IsPublic && !type.IsAbstract && typeof(Control).IsAssignableFrom(type))
            .Where(type => string.Equals(type.Namespace, "CheemsControl", StringComparison.Ordinal))
            .Select(CreateProfile)
            .OrderBy(profile => CategoryOrder(profile.Category))
            .ThenBy(profile => profile.ControlType.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static GifRecordingProfile CreateProfile(Type type)
    {
        if (type.Name.EndsWith("Loader", StringComparison.Ordinal))
        {
            var timing = ResolveLoaderTiming(type);
            return new GifRecordingProfile(
                type,
                "Loaders",
                TimeSpan.FromSeconds(timing.Duration),
                TimeSpan.FromSeconds(timing.Warmup),
                ConfigureCommon,
                control => new PassiveRecordingScript(control));
        }

        if (typeof(ProgressBar).IsAssignableFrom(type))
        {
            return new GifRecordingProfile(
                type,
                "Progress",
                TimeSpan.FromSeconds(2.6),
                TimeSpan.Zero,
                ConfigureProgress,
                control => new ProgressRecordingScript((ProgressBar)control));
        }

        if (typeof(ToggleButton).IsAssignableFrom(type))
        {
            return new GifRecordingProfile(
                type,
                "Inputs",
                TimeSpan.FromSeconds(2.5),
                TimeSpan.Zero,
                ConfigureToggle,
                control => new ToggleRecordingScript((ToggleButton)control));
        }

        if (typeof(ButtonBase).IsAssignableFrom(type))
        {
            return new GifRecordingProfile(
                type,
                "Buttons",
                TimeSpan.FromSeconds(1.6),
                TimeSpan.Zero,
                ConfigureButton,
                control => new ButtonRecordingScript((ButtonBase)control));
        }

        if (type == typeof(CheemsSearchBox))
        {
            return new GifRecordingProfile(
                type,
                "Inputs",
                TimeSpan.FromSeconds(2.9),
                TimeSpan.Zero,
                ConfigureSearchBox,
                control => new SearchBoxRecordingScript((CheemsSearchBox)control));
        }

        if (typeof(TextBoxBase).IsAssignableFrom(type))
        {
            return new GifRecordingProfile(
                type,
                "Inputs",
                TimeSpan.FromSeconds(2.5),
                TimeSpan.Zero,
                ConfigureTextInput,
                control => new TextInputRecordingScript((TextBoxBase)control));
        }

        return new GifRecordingProfile(
            type,
            "Other",
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero,
            ConfigureCommon,
            control => new PassiveRecordingScript(control));
    }

    private static (double Duration, double Warmup) ResolveLoaderTiming(Type type)
    {
        if (LoaderTimings.TryGetValue(type.Name, out var timing))
        {
            return timing;
        }

        var inferredDuration = type
            .GetFields(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(field => field.Name.Contains("Duration", StringComparison.OrdinalIgnoreCase))
            .Select(field => field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null))
            .Select(value => value is IConvertible convertible
                ? convertible.ToDouble(System.Globalization.CultureInfo.InvariantCulture)
                : 0)
            .DefaultIfEmpty(2)
            .Max();
        return (Math.Max(0.1, inferredDuration), 0);
    }

    private static void ConfigureCommon(Control control)
    {
        control.HorizontalAlignment = HorizontalAlignment.Center;
        control.VerticalAlignment = VerticalAlignment.Center;
    }

    private static void ConfigureButton(Control control)
    {
        ConfigureCommon(control);
        if (control is ContentControl contentControl &&
            InitialContent.TryGetValue(control.GetType().Name, out var content))
        {
            contentControl.Content = content;
        }
    }

    private static void ConfigureToggle(Control control)
    {
        ConfigureCommon(control);
        ((ToggleButton)control).IsChecked = false;
    }

    private static void ConfigureProgress(Control control)
    {
        ConfigureCommon(control);
        var progress = (ProgressBar)control;
        progress.Minimum = 0;
        progress.Maximum = 100;
        progress.Value = 0;
    }

    private static void ConfigureTextInput(Control control)
    {
        ConfigureCommon(control);
        if (control is CheemsGlowInput glowInput)
        {
            glowInput.Placeholder = "Type here";
        }
    }

    private static void ConfigureSearchBox(Control control)
    {
        ConfigureCommon(control);
        var searchBox = (CheemsSearchBox)control;
        searchBox.Label = "Search";
        searchBox.Text = string.Empty;
    }

    private static int CategoryOrder(string category) => category switch
    {
        "Buttons" => 0,
        "Loaders" => 1,
        "Inputs" => 2,
        "Progress" => 3,
        _ => 4
    };
}

internal abstract class ControlRecordingScript
{
    protected ControlRecordingScript(Control control) => Control = control;

    protected Control Control { get; }

    public virtual void Start()
    {
    }

    public abstract void Update(TimeSpan elapsed);

    public virtual void Finish()
    {
    }
}

internal sealed class PassiveRecordingScript : ControlRecordingScript
{
    public PassiveRecordingScript(Control control) : base(control)
    {
    }

    public override void Update(TimeSpan elapsed)
    {
    }
}

internal abstract class StagedRecordingScript : ControlRecordingScript
{
    private int _currentStage = -1;

    protected StagedRecordingScript(Control control) : base(control)
    {
    }

    protected abstract IReadOnlyList<TimeSpan> StageTimes { get; }

    protected abstract void EnterStage(int stage);

    public override void Update(TimeSpan elapsed)
    {
        while (_currentStage + 1 < StageTimes.Count && elapsed >= StageTimes[_currentStage + 1])
        {
            _currentStage++;
            EnterStage(_currentStage);
        }
    }
}

internal sealed class ButtonRecordingScript : StagedRecordingScript
{
    private static readonly TimeSpan[] Times =
    {
        TimeSpan.Zero,
        TimeSpan.FromSeconds(0.25),
        TimeSpan.FromSeconds(0.65),
        TimeSpan.FromSeconds(0.85),
        TimeSpan.FromSeconds(1.2)
    };

    private readonly ButtonBase _button;

    public ButtonRecordingScript(ButtonBase button) : base(button) => _button = button;

    protected override IReadOnlyList<TimeSpan> StageTimes => Times;

    protected override void EnterStage(int stage)
    {
        switch (stage)
        {
            case 0:
                RecordingInputState.Reset(_button);
                break;
            case 1:
                RecordingInputState.Enter(_button);
                break;
            case 2:
                RecordingInputState.Press(_button);
                break;
            case 3:
                RecordingInputState.Release(_button, raiseClick: true);
                break;
            case 4:
                RecordingInputState.Leave(_button);
                break;
        }
    }

    public override void Finish() => RecordingInputState.Reset(_button);
}

internal sealed class ToggleRecordingScript : StagedRecordingScript
{
    private static readonly TimeSpan[] Times =
    {
        TimeSpan.Zero,
        TimeSpan.FromSeconds(0.2),
        TimeSpan.FromSeconds(0.5),
        TimeSpan.FromSeconds(0.7),
        TimeSpan.FromSeconds(1.4),
        TimeSpan.FromSeconds(1.6),
        TimeSpan.FromSeconds(2.15)
    };

    private readonly ToggleButton _toggle;

    public ToggleRecordingScript(ToggleButton toggle) : base(toggle) => _toggle = toggle;

    protected override IReadOnlyList<TimeSpan> StageTimes => Times;

    protected override void EnterStage(int stage)
    {
        switch (stage)
        {
            case 0:
                _toggle.IsChecked = false;
                RecordingInputState.Reset(_toggle);
                break;
            case 1:
                RecordingInputState.Enter(_toggle);
                break;
            case 2:
                RecordingInputState.Press(_toggle);
                break;
            case 3:
                RecordingInputState.Release(_toggle, raiseClick: false);
                _toggle.IsChecked = true;
                _toggle.RaiseEvent(new RoutedEventArgs(ToggleButton.CheckedEvent, _toggle));
                break;
            case 4:
                RecordingInputState.Press(_toggle);
                break;
            case 5:
                RecordingInputState.Release(_toggle, raiseClick: false);
                _toggle.IsChecked = false;
                _toggle.RaiseEvent(new RoutedEventArgs(ToggleButton.UncheckedEvent, _toggle));
                break;
            case 6:
                RecordingInputState.Leave(_toggle);
                break;
        }
    }

    public override void Finish()
    {
        _toggle.IsChecked = false;
        RecordingInputState.Reset(_toggle);
    }
}

internal sealed class ProgressRecordingScript : ControlRecordingScript
{
    private const double RampDurationSeconds = 2;
    private readonly ProgressBar _progress;

    public ProgressRecordingScript(ProgressBar progress) : base(progress) => _progress = progress;

    public override void Start() => _progress.Value = _progress.Minimum;

    public override void Update(TimeSpan elapsed)
    {
        var fraction = Math.Clamp(elapsed.TotalSeconds / RampDurationSeconds, 0, 1);
        _progress.Value = _progress.Minimum + ((_progress.Maximum - _progress.Minimum) * fraction);
    }

    public override void Finish() => _progress.Value = _progress.Maximum;
}

internal sealed class TextInputRecordingScript : ControlRecordingScript
{
    private const string DemoText = "Cheems";
    private readonly TextBoxBase _textBox;
    private int _stage;

    public TextInputRecordingScript(TextBoxBase textBox) : base(textBox) => _textBox = textBox;

    public override void Start()
    {
        _stage = 0;
        if (_textBox is TextBox textBox)
        {
            textBox.Text = string.Empty;
        }

        RecordingInputState.Reset(_textBox);
    }

    public override void Update(TimeSpan elapsed)
    {
        var seconds = elapsed.TotalSeconds;
        if (_stage < 1 && seconds >= 0.25)
        {
            RecordingInputState.Enter(_textBox);
            _stage = 1;
        }

        if (_stage < 2 && seconds >= 0.45)
        {
            RecordingInputState.SetKeyboardFocus(_textBox, true);
            _stage = 2;
        }

        if (_textBox is TextBox textBox && seconds is >= 0.6 and < 1.5)
        {
            var characterCount = Math.Clamp((int)Math.Ceiling((seconds - 0.6) / 0.12), 0, DemoText.Length);
            textBox.Text = DemoText[..characterCount];
            textBox.CaretIndex = textBox.Text.Length;
        }

        if (_stage < 3 && seconds >= 1.55)
        {
            if (_textBox is TextBox textBoxToClear)
            {
                textBoxToClear.Text = string.Empty;
            }

            _stage = 3;
        }

        if (_stage < 4 && seconds >= 1.9)
        {
            RecordingInputState.SetKeyboardFocus(_textBox, false);
            RecordingInputState.Leave(_textBox);
            _stage = 4;
        }
    }

    public override void Finish()
    {
        RecordingInputState.SetKeyboardFocus(_textBox, false);
        RecordingInputState.Reset(_textBox);
    }
}

internal sealed class SearchBoxRecordingScript : ControlRecordingScript
{
    private const string DemoText = "Cheems";
    private readonly CheemsSearchBox _searchBox;
    private int _stage;

    public SearchBoxRecordingScript(CheemsSearchBox searchBox) : base(searchBox) => _searchBox = searchBox;

    public override void Start()
    {
        _stage = 0;
        _searchBox.Text = string.Empty;
    }

    public override void Update(TimeSpan elapsed)
    {
        var seconds = elapsed.TotalSeconds;
        if (seconds is >= 0.25 and < 1.15)
        {
            var characterCount = Math.Clamp((int)Math.Ceiling((seconds - 0.25) / 0.12), 0, DemoText.Length);
            _searchBox.Text = DemoText[..characterCount];
        }

        var searchButton = FindTemplateButton("PartSearchButton");
        var clearButton = FindTemplateButton("PartClearButton");

        if (_stage < 1 && seconds >= 1.1 && searchButton is not null)
        {
            RecordingInputState.Enter(searchButton);
            _stage = 1;
        }

        if (_stage < 2 && seconds >= 1.35 && searchButton is not null)
        {
            RecordingInputState.Press(searchButton);
            _stage = 2;
        }

        if (_stage < 3 && seconds >= 1.55 && searchButton is not null)
        {
            RecordingInputState.Release(searchButton, raiseClick: true);
            _stage = 3;
        }

        if (_stage < 4 && seconds >= 1.8 && searchButton is not null && clearButton is not null)
        {
            RecordingInputState.Leave(searchButton);
            RecordingInputState.Enter(clearButton);
            _stage = 4;
        }

        if (_stage < 5 && seconds >= 2.05 && clearButton is not null)
        {
            RecordingInputState.Press(clearButton);
            _stage = 5;
        }

        if (_stage < 6 && seconds >= 2.25 && clearButton is not null)
        {
            RecordingInputState.Release(clearButton, raiseClick: true);
            _stage = 6;
        }

        if (_stage < 7 && seconds >= 2.55 && clearButton is not null)
        {
            RecordingInputState.Leave(clearButton);
            _stage = 7;
        }
    }

    public override void Finish()
    {
        _searchBox.Text = string.Empty;
        if (FindTemplateButton("PartSearchButton") is { } searchButton)
        {
            RecordingInputState.Reset(searchButton);
        }

        if (FindTemplateButton("PartClearButton") is { } clearButton)
        {
            RecordingInputState.Reset(clearButton);
        }
    }

    private Button? FindTemplateButton(string name) =>
        _searchBox.Template?.FindName(name, _searchBox) as Button;
}

internal static class RecordingInputState
{
    private const BindingFlags KeyFlags = BindingFlags.Static | BindingFlags.NonPublic;

    private static readonly DependencyPropertyKey IsMouseOverKey = GetKey(typeof(UIElement), "IsMouseOverPropertyKey");
    private static readonly DependencyPropertyKey IsPressedKey = GetKey(typeof(ButtonBase), "IsPressedPropertyKey");
    private static readonly DependencyPropertyKey IsKeyboardFocusedKey = GetKey(typeof(UIElement), "IsKeyboardFocusedPropertyKey");
    private static readonly DependencyPropertyKey IsKeyboardFocusWithinKey = GetKey(typeof(UIElement), "IsKeyboardFocusWithinPropertyKey");

    public static void Enter(UIElement element)
    {
        element.SetValue(IsMouseOverKey, true);
        element.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)
        {
            RoutedEvent = Mouse.MouseEnterEvent,
            Source = element
        });
        element.SetValue(IsMouseOverKey, true);
    }

    public static void Leave(UIElement element)
    {
        element.SetValue(IsMouseOverKey, false);
        element.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)
        {
            RoutedEvent = Mouse.MouseLeaveEvent,
            Source = element
        });
        element.SetValue(IsMouseOverKey, false);
    }

    public static void Press(ButtonBase button)
    {
        button.SetValue(IsPressedKey, true);
        RaiseMouseButtonEvent(button, Mouse.PreviewMouseDownEvent);
        RaiseMouseButtonEvent(button, Mouse.MouseDownEvent);
        button.SetValue(IsPressedKey, true);
    }

    public static void Release(ButtonBase button, bool raiseClick)
    {
        RaiseMouseButtonEvent(button, Mouse.PreviewMouseUpEvent);
        RaiseMouseButtonEvent(button, Mouse.MouseUpEvent);
        button.ReleaseMouseCapture();
        button.SetValue(IsPressedKey, false);
        if (raiseClick)
        {
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
        }
    }

    public static void SetKeyboardFocus(UIElement element, bool focused)
    {
        element.SetValue(IsKeyboardFocusedKey, focused);
        element.SetValue(IsKeyboardFocusWithinKey, focused);
    }

    public static void Reset(UIElement element)
    {
        if (element is ButtonBase button)
        {
            button.ReleaseMouseCapture();
            button.SetValue(IsPressedKey, false);
        }

        SetKeyboardFocus(element, false);
        element.SetValue(IsMouseOverKey, false);
    }

    private static void RaiseMouseButtonEvent(UIElement element, RoutedEvent routedEvent)
    {
        element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
        {
            RoutedEvent = routedEvent,
            Source = element
        });
    }

    private static DependencyPropertyKey GetKey(Type ownerType, string fieldName)
    {
        return ownerType.GetField(fieldName, KeyFlags)?.GetValue(null) as DependencyPropertyKey
               ?? throw new InvalidOperationException($"无法读取 WPF 状态键 {ownerType.Name}.{fieldName}。");
    }
}
