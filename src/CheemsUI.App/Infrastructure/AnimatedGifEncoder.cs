using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CheemsUI.App.Infrastructure;

/// <summary>
/// 基于 WPF/WIC 的轻量动画 GIF 编码器，支持色键透明。
/// </summary>
internal static class AnimatedGifEncoder
{
    public static void Save(
        string filePath,
        IReadOnlyList<BitmapSource> frames,
        int framesPerSecond,
        Color? chromaKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(framesPerSecond);

        if (frames.Count == 0)
        {
            throw new ArgumentException("动画 GIF 至少需要一帧。", nameof(frames));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var processedFrames = chromaKey.HasValue
            ? frames.Select(f => ConvertToTransparent(f, chromaKey.Value)).ToArray()
            : frames.ToArray();

        var encoder = new GifBitmapEncoder();
        foreach (var frame in processedFrames)
        {
            encoder.Frames.Add(BitmapFrame.Create(frame));
        }

        using var encodedStream = new MemoryStream();
        encoder.Save(encodedStream);
        var sourceBytes = encodedStream.ToArray();

        var animatedBytes = chromaKey.HasValue
            ? AddAnimationMetadataWithTransparency(sourceBytes, frames.Count, framesPerSecond)
            : AddAnimationMetadata(sourceBytes, frames.Count, framesPerSecond);

        File.WriteAllBytes(filePath, animatedBytes);
    }

    /// <summary>
    /// 将帧转换为 Bgra32 格式，色键像素的 Alpha 设为 0（透明）。
    /// </summary>
    private static BitmapSource ConvertToTransparent(BitmapSource source, Color chromaKey)
    {
        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        var pixelWidth = converted.PixelWidth;
        var pixelHeight = converted.PixelHeight;
        var stride = pixelWidth * 4;
        var pixels = new byte[stride * pixelHeight];
        converted.CopyPixels(pixels, stride, 0);

        var keyR = chromaKey.R;
        var keyG = chromaKey.G;
        var keyB = chromaKey.B;

        for (var i = 0; i < pixels.Length; i += 4)
        {
            var b = pixels[i];
            var g = pixels[i + 1];
            var r = pixels[i + 2];

            // 色键匹配：将该像素设为完全透明
            if (r == keyR && g == keyG && b == keyB)
            {
                pixels[i] = 0;     // B
                pixels[i + 1] = 0; // G
                pixels[i + 2] = 0; // R
                pixels[i + 3] = 0; // A = 0 (透明)
            }
            else
            {
                pixels[i + 3] = 255; // A = 255 (不透明)
            }
        }

        var bitmap = BitmapSource.Create(
            pixelWidth, pixelHeight,
            96, 96,
            PixelFormats.Bgra32,
            null,
            pixels, stride);

        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>
    /// 添加动画元数据（无透明支持）。
    /// </summary>
    private static byte[] AddAnimationMetadata(byte[] source, int expectedFrameCount, int framesPerSecond)
    {
        return PatchAnimationMetadata(source, expectedFrameCount, framesPerSecond, fixTransparency: false);
    }

    /// <summary>
    /// 添加动画元数据并修正透明色索引。
    /// </summary>
    private static byte[] AddAnimationMetadataWithTransparency(byte[] source, int expectedFrameCount, int framesPerSecond)
    {
        return PatchAnimationMetadata(source, expectedFrameCount, framesPerSecond, fixTransparency: true);
    }

    private static byte[] PatchAnimationMetadata(byte[] source, int expectedFrameCount, int framesPerSecond, bool fixTransparency)
    {
        if (source.Length < 14 || Encoding.ASCII.GetString(source, 0, 3) != "GIF")
        {
            throw new InvalidDataException("WIC 返回了无效的 GIF 数据。");
        }

        // 找到透明色索引（调色板中 Alpha=0 的颜色）
        var transparentIndex = fixTransparency ? FindTransparentIndex(source) : -1;

        using var output = new MemoryStream(source.Length + 19 + (expectedFrameCount * 8));
        var position = 0;

        // Header + Logical Screen Descriptor + optional Global Color Table.
        var headerLength = 13;
        var packed = source[10];
        if ((packed & 0x80) != 0)
        {
            headerLength += 3 * (1 << ((packed & 0x07) + 1));
        }

        output.Write(source, 0, headerLength);
        position = headerLength;
        WriteLoopExtension(output);

        var frameIndex = 0;
        var hasPendingGraphicControl = false;
        while (position < source.Length)
        {
            switch (source[position])
            {
                case 0x21: // Extension
                    EnsureAvailable(source, position, 2);
                    if (source[position + 1] == 0xF9)
                    {
                        EnsureAvailable(source, position, 8);
                        var block = source.AsSpan(position, 8).ToArray();
                        block[3] = (byte)((block[3] & 0xE3) | 0x08); // disposal=restore background

                        // 设置透明标志
                        if (transparentIndex >= 0)
                        {
                            block[3] = (byte)(block[3] | 0x01); // transparent color flag
                            block[6] = (byte)transparentIndex;
                        }

                        var delay = GetFrameDelay(frameIndex, framesPerSecond);
                        block[4] = (byte)(delay & 0xFF);
                        block[5] = (byte)(delay >> 8);
                        output.Write(block);
                        position += 8;
                        hasPendingGraphicControl = true;
                    }
                    else
                    {
                        position = CopyExtension(source, position, output);
                    }

                    break;

                case 0x2C: // Image Descriptor
                    if (!hasPendingGraphicControl)
                    {
                        WriteGraphicControlExtension(output, GetFrameDelay(frameIndex, framesPerSecond), transparentIndex);
                    }

                    position = CopyImage(source, position, output);
                    frameIndex++;
                    hasPendingGraphicControl = false;
                    break;

                case 0x3B: // Trailer
                    output.WriteByte(0x3B);
                    position++;
                    break;

                default:
                    throw new InvalidDataException($"无法识别 GIF 数据块 0x{source[position]:X2}。");
            }
        }

        if (frameIndex != expectedFrameCount)
        {
            throw new InvalidDataException($"GIF 帧数不一致：预期 {expectedFrameCount}，实际 {frameIndex}。");
        }

        return output.ToArray();
    }

    /// <summary>
    /// 在 Global Color Table 中找到 Alpha=0 的透明色索引。
    /// WIC 量化器通常会把透明色放在调色板中。
    /// </summary>
    private static int FindTransparentIndex(byte[] gif)
    {
        var packed = gif[10];
        if ((packed & 0x80) == 0)
        {
            return -1; // 无 Global Color Table
        }

        var tableSize = 1 << ((packed & 0x07) + 1);
        var tableStart = 13;

        // 遍历 GCT，找黑色(0,0,0)——WIC 通常把透明像素量化为黑色
        for (var i = 0; i < tableSize; i++)
        {
            var offset = tableStart + i * 3;
            if (offset + 2 < gif.Length &&
                gif[offset] == 0 && gif[offset + 1] == 0 && gif[offset + 2] == 0)
            {
                return i;
            }
        }

        return 0; // 默认使用索引 0
    }

    private static int CopyExtension(byte[] source, int position, Stream output)
    {
        output.Write(source, position, 2);
        position += 2;
        return CopySubBlocks(source, position, output);
    }

    private static int CopyImage(byte[] source, int position, Stream output)
    {
        EnsureAvailable(source, position, 10);
        var descriptorLength = 10;
        var packed = source[position + 9];
        if ((packed & 0x80) != 0)
        {
            descriptorLength += 3 * (1 << ((packed & 0x07) + 1));
        }

        EnsureAvailable(source, position, descriptorLength + 1);
        output.Write(source, position, descriptorLength + 1); // descriptor/table + LZW minimum code size
        position += descriptorLength + 1;
        return CopySubBlocks(source, position, output);
    }

    private static int CopySubBlocks(byte[] source, int position, Stream output)
    {
        while (true)
        {
            EnsureAvailable(source, position, 1);
            var length = source[position];
            EnsureAvailable(source, position, length + 1);
            output.Write(source, position, length + 1);
            position += length + 1;
            if (length == 0)
            {
                return position;
            }
        }
    }

    private static void WriteLoopExtension(Stream output)
    {
        output.WriteByte(0x21);
        output.WriteByte(0xFF);
        output.WriteByte(0x0B);
        output.Write(Encoding.ASCII.GetBytes("NETSCAPE2.0"));
        output.Write(new byte[] { 0x03, 0x01, 0x00, 0x00, 0x00 });
    }

    private static void WriteGraphicControlExtension(Stream output, int delay, int transparentIndex)
    {
        var flags = (byte)0x08; // disposal=restore background
        if (transparentIndex >= 0)
        {
            flags = (byte)(flags | 0x01); // transparent color flag
        }

        output.Write(new byte[]
        {
            0x21, 0xF9, 0x04,
            flags,
            (byte)(delay & 0xFF),
            (byte)(delay >> 8),
            (byte)Math.Max(0, transparentIndex),
            0x00
        });
    }

    private static int GetFrameDelay(int frameIndex, int framesPerSecond)
    {
        // GIF 延迟单位是 1/100 秒。通过误差累积生成稳定序列。
        var currentHundredths = (int)Math.Round((frameIndex + 1) * 100d / framesPerSecond);
        var previousHundredths = (int)Math.Round(frameIndex * 100d / framesPerSecond);
        return Math.Max(1, currentHundredths - previousHundredths);
    }

    private static void EnsureAvailable(byte[] source, int position, int requiredLength)
    {
        if (position < 0 || requiredLength < 0 || position + requiredLength > source.Length)
        {
            throw new InvalidDataException("GIF 数据块不完整。");
        }
    }
}
