using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CheemsUI.App.Infrastructure;

/// <summary>
/// 录制虚拟光标：素材（输出目录 Assets\cursor.png，替换该文件后重新导出即可换光标）与抓帧后合成。
/// 光标不进 WPF 视觉树——叠加元素会扰动 DropShadowEffect 等效果的光栅化缓存，
/// 导致控件离开过渡后无法回到初始像素、GIF 循环出现跳变；
/// 改为抓帧后用 GDI+ 按时间确定性合成，画面与光标完全解耦。
/// </summary>
internal static class RecordingCursor
{
    private const string CursorFileName = "cursor.png";
    private static readonly object Gate = new();
    private static BitmapSource? _wpfBitmap;
    private static Bitmap? _gdiBitmap;

    /// <summary>素材是否可用（缺失时录制不带光标，不影响导出）。</summary>
    public static bool IsAvailable => LoadGdi() is not null;

    private static Bitmap? LoadGdi()
    {
        lock (Gate)
        {
            if (_gdiBitmap is not null) return _gdiBitmap;
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", CursorFileName);
            if (!File.Exists(path)) return null;
            _gdiBitmap = new Bitmap(path);
            return _gdiBitmap;
        }
    }

    /// <summary>
    /// 把光标合成到抓取帧上。<paramref name="tipPositionDip"/> 为光标尖端在舞台坐标（DIP）中的位置，
    /// <paramref name="scale"/> 为抓取帧像素/DIP 的缩放比。
    /// </summary>
    public static BitmapSource Composite(BitmapSource frame, System.Windows.Point tipPositionDip, double scale)
    {
        var cursor = LoadGdi();
        if (cursor is null) return frame;

        var source = new FormatConvertedBitmap(frame, System.Windows.Media.PixelFormats.Bgra32, null, 0);
        var width = source.PixelWidth;
        var height = source.PixelHeight;
        var stride = width * 4;
        var pixels = new byte[stride * height];
        source.CopyPixels(pixels, stride, 0);

        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            using var bitmap = new Bitmap(width, height, stride, PixelFormat.Format32bppArgb, handle.AddrOfPinnedObject());
            using var graphics = Graphics.FromImage(bitmap);

            // 箭头尖端在素材左上角 (1,1) 处，绘制偏移一个热点位让尖端落在目标坐标上。
            // 光标位图被多个并行编码任务共享，GDI+ 对象非线程安全，绘制需串行（临界区仅微秒级）。
            var x = (float)(tipPositionDip.X * scale - scale);
            var y = (float)(tipPositionDip.Y * scale - scale);
            var size = (float)(32 * scale);
            lock (Gate)
            {
                graphics.DrawImage(cursor, x, y, size, size);
            }

            var result = BitmapSource.Create(
                width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, pixels, stride);
            result.Freeze();
            return result;
        }
        finally
        {
            handle.Free();
        }
    }
}
