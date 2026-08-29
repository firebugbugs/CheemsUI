using System.Windows.Media;
using CheemsUI.App.Infrastructure;

namespace CheemsUI.App.ViewModels;

/// <summary>
/// 一张背景卡片的独立参数。共享参数始终可用，特色参数由背景类型决定是否展示。
/// </summary>
public sealed class BackgroundProfileViewModel : ObservableObject
{
    private double _backgroundOpacity = 0.8;
    private string _primaryColorText;
    private Color _primaryColor;
    private bool _isPrimaryColorValid = true;
    private double _animationSpeed = 1;
    private bool _isAnimationEnabled = true;
    private double _risoBackgroundAlpha = 1;
    private int _risoPixelSize = 4;
    private int _risoLevels = 6;
    private double _risoScale = 1.5;
    private double _risoContrast = 1.2;
    private double _risoFlowAngle = 30;
    private double _risoDetail = 0.4;
    private double _risoGlow = 0.5;
    private string _birdsBackgroundColorText = "#07190F";
    private Color _birdsBackgroundColor = (Color)ColorConverter.ConvertFromString("#07190F")!;
    private double _birdsBackgroundAlpha = 1;
    private string _birdsSecondaryColorText;
    private Color _birdsSecondaryColor;
    private double _birdsSize = 1;
    private double _birdsWingSpan = 30;
    private double _birdsSpeedLimit = 5;
    private double _birdsSeparation = 20;
    private double _birdsAlignment = 20;
    private double _birdsCohesion = 20;
    private int _birdsQuantity = 5;
    public BackgroundProfileViewModel(string key, string displayName, string primaryColor)
    {
        Key = key;
        DisplayName = displayName;
        _primaryColorText = primaryColor;
        _primaryColor = (Color)ColorConverter.ConvertFromString(primaryColor)!;
        _birdsSecondaryColorText = primaryColor;
        _birdsSecondaryColor = _primaryColor;
    }

    public string Key { get; }

    public string DisplayName { get; }

    public bool SupportsRisoSettings => string.Equals(Key, "RisoDither", StringComparison.Ordinal);

    public bool SupportsBirdsSettings => string.Equals(Key, "Birds", StringComparison.Ordinal);

    public double BackgroundOpacity
    {
        get => _backgroundOpacity;
        set => SetProperty(ref _backgroundOpacity, Math.Clamp(value, 0.1, 1));
    }

    public string PrimaryColorText
    {
        get => _primaryColorText;
        set
        {
            if (!SetProperty(ref _primaryColorText, value)) return;
            try
            {
                _primaryColor = (Color)ColorConverter.ConvertFromString(value)!;
                OnPropertyChanged(nameof(PrimaryColor));
                IsPrimaryColorValid = true;
            }
            catch (Exception exception) when (exception is FormatException or NotSupportedException or ArgumentException)
            {
                IsPrimaryColorValid = false;
            }
        }
    }

    public Color PrimaryColor
    {
        get => _primaryColor;
        set
        {
            if (!SetProperty(ref _primaryColor, value)) return;
            _primaryColorText = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
            OnPropertyChanged(nameof(PrimaryColorText));
            IsPrimaryColorValid = true;
        }
    }

    public bool IsPrimaryColorValid
    {
        get => _isPrimaryColorValid;
        private set => SetProperty(ref _isPrimaryColorValid, value);
    }

    public double AnimationSpeed
    {
        get => _animationSpeed;
        set => SetProperty(ref _animationSpeed, Math.Clamp(value, 0.02, 2));
    }

    public bool IsAnimationEnabled
    {
        get => _isAnimationEnabled;
        set => SetProperty(ref _isAnimationEnabled, value);
    }

    public double RisoBackgroundAlpha
    {
        get => _risoBackgroundAlpha;
        set => SetProperty(ref _risoBackgroundAlpha, Math.Clamp(value, 0, 1));
    }

    public int RisoPixelSize
    {
        get => _risoPixelSize;
        set => SetProperty(ref _risoPixelSize, Math.Clamp(value, 1, 16));
    }

    public int RisoLevels
    {
        get => _risoLevels;
        set => SetProperty(ref _risoLevels, Math.Clamp(value, 2, 16));
    }

    public double RisoScale
    {
        get => _risoScale;
        set => SetProperty(ref _risoScale, Math.Clamp(value, 0.3, 5));
    }

    public double RisoContrast
    {
        get => _risoContrast;
        set => SetProperty(ref _risoContrast, Math.Clamp(value, 0.4, 2.5));
    }

    public double RisoFlowAngle
    {
        get => _risoFlowAngle;
        set => SetProperty(ref _risoFlowAngle, Math.Clamp(value, 0, 360));
    }

    public double RisoDetail
    {
        get => _risoDetail;
        set => SetProperty(ref _risoDetail, Math.Clamp(value, 0, 1));
    }

    public double RisoGlow
    {
        get => _risoGlow;
        set => SetProperty(ref _risoGlow, Math.Clamp(value, 0, 1));
    }

    public string BirdsBackgroundColorText
    {
        get => _birdsBackgroundColorText;
        set
        {
            if (!SetProperty(ref _birdsBackgroundColorText, value)) return;
            if (TryParseColor(value, out var color)) BirdsBackgroundColor = color;
        }
    }

    public Color BirdsBackgroundColor
    {
        get => _birdsBackgroundColor;
        set
        {
            if (!SetProperty(ref _birdsBackgroundColor, value)) return;
            _birdsBackgroundColorText = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
            OnPropertyChanged(nameof(BirdsBackgroundColorText));
        }
    }

    public double BirdsBackgroundAlpha { get => _birdsBackgroundAlpha; set => SetProperty(ref _birdsBackgroundAlpha, Math.Clamp(value, 0, 1)); }

    public string BirdsSecondaryColorText
    {
        get => _birdsSecondaryColorText;
        set
        {
            if (!SetProperty(ref _birdsSecondaryColorText, value)) return;
            if (TryParseColor(value, out var color)) BirdsSecondaryColor = color;
        }
    }

    public Color BirdsSecondaryColor
    {
        get => _birdsSecondaryColor;
        set
        {
            if (!SetProperty(ref _birdsSecondaryColor, value)) return;
            _birdsSecondaryColorText = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
            OnPropertyChanged(nameof(BirdsSecondaryColorText));
        }
    }

    public double BirdsSize { get => _birdsSize; set => SetProperty(ref _birdsSize, Math.Clamp(value, 0.2, 4)); }
    public double BirdsWingSpan { get => _birdsWingSpan; set => SetProperty(ref _birdsWingSpan, Math.Clamp(value, 5, 80)); }
    public double BirdsSpeedLimit { get => _birdsSpeedLimit; set => SetProperty(ref _birdsSpeedLimit, Math.Clamp(value, 0.1, 12)); }
    public double BirdsSeparation { get => _birdsSeparation; set => SetProperty(ref _birdsSeparation, Math.Clamp(value, 1, 80)); }
    public double BirdsAlignment { get => _birdsAlignment; set => SetProperty(ref _birdsAlignment, Math.Clamp(value, 1, 80)); }
    public double BirdsCohesion { get => _birdsCohesion; set => SetProperty(ref _birdsCohesion, Math.Clamp(value, 1, 80)); }
    // Vanta BIRDS 把 quantity 当作纹理边长指数；5 已对应 1,024 只鸟，继续增大会急剧掉帧。
    public int BirdsQuantity { get => _birdsQuantity; set => SetProperty(ref _birdsQuantity, Math.Clamp(value, 1, 5)); }

    private static bool TryParseColor(string value, out Color color)
    {
        try
        {
            color = (Color)ColorConverter.ConvertFromString(value)!;
            return true;
        }
        catch (Exception exception) when (exception is FormatException or NotSupportedException or ArgumentException)
        {
            color = default;
            return false;
        }
    }

}
