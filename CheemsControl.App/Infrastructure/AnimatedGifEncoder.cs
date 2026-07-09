using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace CheemsControl.App.Infrastructure;

/// <summary>
/// 基于 WPF/WIC 的轻量动画 GIF 编码器，不引入第三方依赖。
/// </summary>
internal static class AnimatedGifEncoder
{
    public static void Save(string filePath, IReadOnlyList<BitmapSource> frames, int framesPerSecond)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(framesPerSecond);

        if (frames.Count == 0)
        {
            throw new ArgumentException("动画 GIF 至少需要一帧。", nameof(frames));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var encoder = new GifBitmapEncoder();
        foreach (var frame in frames)
        {
            encoder.Frames.Add(BitmapFrame.Create(frame));
        }

        using var encodedStream = new MemoryStream();
        encoder.Save(encodedStream);
        var animatedBytes = AddAnimationMetadata(
            encodedStream.ToArray(),
            frames.Count,
            framesPerSecond);
        File.WriteAllBytes(filePath, animatedBytes);
    }

    private static byte[] AddAnimationMetadata(byte[] source, int expectedFrameCount, int framesPerSecond)
    {
        if (source.Length < 14 || Encoding.ASCII.GetString(source, 0, 3) != "GIF")
        {
            throw new InvalidDataException("WIC 返回了无效的 GIF 数据。");
        }

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
                        WriteGraphicControlExtension(output, GetFrameDelay(frameIndex, framesPerSecond));
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

    private static void WriteGraphicControlExtension(Stream output, int delay)
    {
        output.Write(new byte[]
        {
            0x21, 0xF9, 0x04,
            0x08,
            (byte)(delay & 0xFF),
            (byte)(delay >> 8),
            0x00, 0x00
        });
    }

    private static int GetFrameDelay(int frameIndex, int framesPerSecond)
    {
        // GIF 延迟单位是 1/100 秒。通过误差累积，在 12 FPS 下生成 8/8/9 的稳定序列。
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
