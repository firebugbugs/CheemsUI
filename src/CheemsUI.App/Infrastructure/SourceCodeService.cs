using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;

namespace CheemsUI.App.Infrastructure;

/// <summary>
/// 示例源码加载与复制（规矩 M6）：源码资源读取、剪贴板操作只允许经此类。
/// Source 为 pack 相对 URI，如 "/CheemsUI.App;component/Sources/Buttons/Basic.xaml.txt"。
/// </summary>
internal static class SourceCodeService
{
    private const uint ClipboardFormatUnicodeText = 13;
    private const uint GlobalMemoryMoveable = 0x0002;
    private const uint GlobalMemoryZeroInitialize = 0x0040;

    private static readonly Dictionary<string, string> Cache = new();

    public static string Load(string sourceUri)
    {
        if (Cache.TryGetValue(sourceUri, out var cached))
        {
            return cached;
        }

        var resource = Application.GetResourceStream(new Uri(sourceUri, UriKind.Relative));
        if (resource is null)
        {
            return $"<!-- 未找到源码资源：{sourceUri} -->";
        }

        using var reader = new StreamReader(resource.Stream);
        var code = reader.ReadToEnd();
        Cache[sourceUri] = code;
        return code;
    }

    /// <summary>
    /// 将 Unicode 文本复制到系统剪贴板。
    /// 不调用 WPF Clipboard.SetDataObject(copy:true)，避免 OleFlushClipboard 在 UI 线程无限等待。
    /// Win32 SetClipboardData 会把 HGLOBAL 所有权转交给系统，因此应用退出后文本仍然有效。
    /// </summary>
    public static Task<bool> TryCopyToClipboardAsync(string text, IntPtr ownerWindow)
    {
        return Task.Run(() => TryCopyToClipboardCore(text, ownerWindow));
    }

    private static bool TryCopyToClipboardCore(string text, IntPtr ownerWindow)
    {
        const int maxAttempts = 8;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (TrySetUnicodeText(text, ownerWindow))
            {
                return true;
            }

            if (attempt + 1 < maxAttempts)
            {
                // OpenClipboard 在被占用时立即失败；等待放在后台线程，不阻塞 WPF UI。
                Thread.Sleep(25 * (attempt + 1));
            }
        }

        return false;
    }

    private static bool TrySetUnicodeText(string text, IntPtr ownerWindow)
    {
        if (!OpenClipboard(ownerWindow))
        {
            return false;
        }

        IntPtr globalMemory = IntPtr.Zero;
        var ownershipTransferred = false;

        try
        {
            if (!EmptyClipboard())
            {
                return false;
            }

            var bytes = Encoding.Unicode.GetBytes(text + '\0');
            globalMemory = GlobalAlloc(
                GlobalMemoryMoveable | GlobalMemoryZeroInitialize,
                (UIntPtr)bytes.Length);
            if (globalMemory == IntPtr.Zero)
            {
                return false;
            }

            var target = GlobalLock(globalMemory);
            if (target == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                Marshal.Copy(bytes, 0, target, bytes.Length);
            }
            finally
            {
                _ = GlobalUnlock(globalMemory);
            }

            if (SetClipboardData(ClipboardFormatUnicodeText, globalMemory) == IntPtr.Zero)
            {
                return false;
            }

            // SetClipboardData 成功后内存归系统所有，调用方不得再释放。
            ownershipTransferred = true;
            return true;
        }
        finally
        {
            if (!ownershipTransferred && globalMemory != IntPtr.Zero)
            {
                _ = GlobalFree(globalMemory);
            }

            _ = CloseClipboard();
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(IntPtr newOwner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint format, IntPtr memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(IntPtr memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr memory);
}
