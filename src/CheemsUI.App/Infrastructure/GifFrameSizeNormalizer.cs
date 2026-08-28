using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CheemsUI.App.Infrastructure;

/// <summary>
/// 将导出的 GIF 吸附到统一的高度档位，避免不同控件产生过于零散的成品尺寸。
/// 宽度始终按相同比例缩放，因此不会拉伸或裁切控件画面。
/// </summary>
internal static class GifFrameSizeNormalizer
{
    // 80px 覆盖紧凑型开关；其余档位兼顾常规控件与较大的 Loader。
    private static readonly int[] HeightTiers = [80, 100, 120, 140, 160, 200, 240, 280];

    public static IReadOnlyList<BitmapSource> NormalizeHeights(IReadOnlyList<BitmapSource> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        if (frames.Count == 0)
        {
            return frames;
        }

        var targetHeight = GetNearestHeightTier(frames[0].PixelHeight);
        if (frames.All(frame => frame.PixelHeight == targetHeight))
        {
            return frames;
        }

        return frames.Select(frame => Resize(frame, targetHeight)).ToArray();
    }

    private static int GetNearestHeightTier(int sourceHeight) => HeightTiers
        .OrderBy(tier => Math.Abs(tier - sourceHeight))
        .ThenBy(tier => tier)
        .First();

    private static BitmapSource Resize(BitmapSource source, int targetHeight)
    {
        if (source.PixelHeight == targetHeight)
        {
            return source;
        }

        var scale = targetHeight / (double)source.PixelHeight;
        var targetWidth = Math.Max(1, (int)Math.Round(
            source.PixelWidth * scale, MidpointRounding.AwayFromZero));
        var resized = new TransformedBitmap(source, new ScaleTransform(
            targetWidth / (double)source.PixelWidth,
            targetHeight / (double)source.PixelHeight));
        resized.Freeze();
        return resized;
    }
}
