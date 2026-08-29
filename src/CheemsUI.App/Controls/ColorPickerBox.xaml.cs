using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CheemsUI.App.Controls;

/// <summary>
/// 应用内通用取色器：常驻仅显示颜色方块，点击后在鼠标旁弹出预设色板、HSV 色盘、亮度条与十六进制输入，
/// 通过 <see cref="SelectedColor"/> 双向绑定。
/// </summary>
public partial class ColorPickerBox : UserControl
{
    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
        nameof(SelectedColor), typeof(Color), typeof(ColorPickerBox),
        new FrameworkPropertyMetadata(
            Color.FromRgb(0, 0, 0),
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, e) => ((ColorPickerBox)d).OnSelectedColorChanged((Color)e.NewValue)));

    private const double WheelSize = 176;
    private const double WheelRadius = 80;
    private const double BrightnessTrackWidth = WheelSize;

    // 首四个为四张背景卡的默认主色，其余为常用补充色。
    private static readonly string[] PresetColors =
    {
        "#FF3F81", "#68B8D7", "#A4E34F", "#8B5CF6", "#E91E63", "#F44336",
        "#FF9800", "#FFC107", "#4CAF50", "#00BCD4", "#2196F3", "#3F51B5",
        "#795548", "#5F6368", "#9E9E9E", "#FFFFFF", "#202124", "#000000"
    };

    private readonly SolidColorBrush _swatchBrush = new(Color.FromRgb(0, 0, 0));
    private readonly LinearGradientBrush _brightnessGradient = new()
    {
        StartPoint = new Point(0, 0.5),
        EndPoint = new Point(1, 0.5)
    };

    private double _hue;
    private double _saturation;
    private double _value = 1;
    private bool _updatingHex;
    private long _popupClosedAt;

    public ColorPickerBox()
    {
        InitializeComponent();

        _brightnessGradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0));
        _brightnessGradient.GradientStops.Add(new GradientStop(Color.FromRgb(255, 255, 255), 1));
        PartBrightnessBar.Background = _brightnessGradient;
        PartSwatchButton.Background = _swatchBrush;
        PartPreviewSwatch.Background = _swatchBrush;
        PartWheelImage.Source = CreateWheelBitmap();

        foreach (var hex in PresetColors)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            var button = new Button
            {
                Tag = hex,
                ToolTip = hex,
                Style = (Style)FindResource("ColorPicker.SwatchButtonStyle"),
                Background = new SolidColorBrush(color)
            };
            button.Click += Preset_Click;
            PartPresetGrid.Children.Add(button);
        }

        OnSelectedColorChanged(SelectedColor);
    }

    public Color SelectedColor
    {
        get => (Color)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    private void OnSelectedColorChanged(Color color)
    {
        (_hue, _saturation, _value) = ColorToHsv(color);
        _swatchBrush.Color = color;
        _brightnessGradient.GradientStops[1].Color = HsvToColor(_hue, _saturation, 1);
        UpdateWheelThumb();
        UpdateBrightnessThumb();

        _updatingHex = true;
        PartHexBox.Text = ToHex(color);
        PartHexBox.Tag = null;
        _updatingHex = false;
    }

    private void SwatchButton_Click(object sender, RoutedEventArgs e)
    {
        // 点击方块时若弹窗刚因本次点击关闭（StaysOpen=False 的外部点击），不再立刻重开。
        if (Environment.TickCount64 - _popupClosedAt < 200)
        {
            return;
        }

        PartPickerPopup.IsOpen = true;
    }

    private void PickerPopup_Closed(object sender, EventArgs e) => _popupClosedAt = Environment.TickCount64;

    private void UpdateWheelThumb()
    {
        var angle = _hue * Math.PI / 180.0;
        var radius = _saturation * WheelRadius;
        Canvas.SetLeft(PartWheelThumb, WheelSize / 2 + radius * Math.Cos(angle) - PartWheelThumb.Width / 2);
        Canvas.SetTop(PartWheelThumb, WheelSize / 2 + radius * Math.Sin(angle) - PartWheelThumb.Height / 2);
    }

    private void UpdateBrightnessThumb()
    {
        Canvas.SetLeft(PartBrightnessThumb, Math.Clamp(_value, 0, 1) * BrightnessTrackWidth - PartBrightnessThumb.Width / 2);
    }

    private void WheelSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var surface = (UIElement)sender;
        surface.CaptureMouse();
        UpdateWheelFromPoint(e.GetPosition(surface));
        e.Handled = true;
    }

    private void WheelSurface_MouseMove(object sender, MouseEventArgs e)
    {
        if (((UIElement)sender).IsMouseCaptured)
        {
            UpdateWheelFromPoint(e.GetPosition((UIElement)sender));
        }
    }

    private void WheelSurface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ((UIElement)sender).ReleaseMouseCapture();
    }

    private void UpdateWheelFromPoint(Point point)
    {
        var dx = point.X - WheelSize / 2;
        var dy = point.Y - WheelSize / 2;
        var hue = (Math.Atan2(dy, dx) * 180.0 / Math.PI + 360.0) % 360.0;
        var saturation = Math.Min(1.0, Math.Sqrt(dx * dx + dy * dy) / WheelRadius);
        SelectedColor = HsvToColor(hue, saturation, _value);
    }

    private void BrightnessTrack_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var track = (UIElement)sender;
        track.CaptureMouse();
        UpdateBrightnessFromPoint(e.GetPosition(track));
        e.Handled = true;
    }

    private void BrightnessTrack_MouseMove(object sender, MouseEventArgs e)
    {
        if (((UIElement)sender).IsMouseCaptured)
        {
            UpdateBrightnessFromPoint(e.GetPosition((UIElement)sender));
        }
    }

    private void BrightnessTrack_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ((UIElement)sender).ReleaseMouseCapture();
    }

    private void UpdateBrightnessFromPoint(Point point)
    {
        var value = Math.Clamp(point.X / BrightnessTrackWidth, 0, 1);
        SelectedColor = HsvToColor(_hue, _saturation, value);
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string hex } && TryParseHex(hex, out var color))
        {
            SelectedColor = color;
        }
    }

    private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingHex)
        {
            return;
        }

        if (TryParseHex(PartHexBox.Text, out var color))
        {
            PartHexBox.Tag = null;
            if (color != SelectedColor)
            {
                SelectedColor = color;
            }
        }
        else
        {
            PartHexBox.Tag = "Invalid";
        }
    }

    private static WriteableBitmap CreateWheelBitmap()
    {
        var size = (int)WheelSize;
        var bitmap = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new int[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x + 0.5 - WheelSize / 2;
                var dy = y + 0.5 - WheelSize / 2;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance > WheelRadius + 1)
                {
                    continue;
                }

                var hue = (Math.Atan2(dy, dx) * 180.0 / Math.PI + 360.0) % 360.0;
                var saturation = Math.Min(1.0, distance / WheelRadius);
                var color = HsvToColor(hue, saturation, 1);
                // 半径边缘做 1px 线性 alpha 过渡，避免锯齿。
                var alpha = (byte)(Math.Clamp(WheelRadius + 0.5 - distance, 0, 1) * 255);
                // Bgra32 按 int 写入时是小端字节序：B 占最低位，A 占最高位。
                pixels[y * size + x] = (alpha << 24) | (color.R << 16) | (color.G << 8) | color.B;
            }
        }

        bitmap.WritePixels(new Int32Rect(0, 0, size, size), pixels, size * 4, 0);
        bitmap.Freeze();
        return bitmap;
    }

    private static bool TryParseHex(string? text, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var value = text.Trim();
        if (value.StartsWith('#'))
        {
            value = value[1..];
        }

        if (value.Length is not (6 or 8) ||
            !uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var number))
        {
            return false;
        }

        color = value.Length == 6
            ? Color.FromRgb((byte)(number >> 16), (byte)(number >> 8), (byte)number)
            : Color.FromArgb((byte)(number >> 24), (byte)(number >> 16), (byte)(number >> 8), (byte)number);
        return true;
    }

    private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static (double Hue, double Saturation, double Value) ColorToHsv(Color color)
    {
        var r = color.R / 255.0;
        var g = color.G / 255.0;
        var b = color.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        double hue;
        if (delta <= 0)
        {
            hue = 0;
        }
        else if (max == r)
        {
            hue = 60.0 * (((g - b) / delta) % 6);
        }
        else if (max == g)
        {
            hue = 60.0 * ((b - r) / delta + 2);
        }
        else
        {
            hue = 60.0 * ((r - g) / delta + 4);
        }

        return ((hue + 360.0) % 360.0, max <= 0 ? 0 : delta / max, max);
    }

    private static Color HsvToColor(double hue, double saturation, double value)
    {
        hue = (hue % 360 + 360) % 360;
        var chroma = value * saturation;
        var secondary = chroma * (1 - Math.Abs(hue / 60 % 2 - 1));
        var match = value - chroma;
        var (r, g, b) = hue switch
        {
            < 60 => (chroma, secondary, 0d),
            < 120 => (secondary, chroma, 0d),
            < 180 => (0d, chroma, secondary),
            < 240 => (0d, secondary, chroma),
            < 300 => (secondary, 0d, chroma),
            _ => (chroma, 0d, secondary)
        };
        return Color.FromRgb(
            (byte)Math.Round((r + match) * 255),
            (byte)Math.Round((g + match) * 255),
            (byte)Math.Round((b + match) * 255));
    }
}
