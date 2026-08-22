using System.Net.Http.Headers;
using System.Net.Http;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace CheemsUI.App.Infrastructure.Updates;

/// <summary>
/// Retrieves published installers from the project's Gitee releases feed. An installer is
/// accepted only when its adjacent .sha256 release asset is present and matches the download.
/// </summary>
internal sealed class UpdateService
{
    private const string ReleasesUrl = "https://gitee.com/api/v5/repos/unbengable/cheems-ui/releases?per_page=100&page=1";
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.GetAsync(ReleasesUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var releases = await JsonSerializer.DeserializeAsync(stream, UpdateJsonContext.Default.ListGiteeRelease, cancellationToken)
                           ?? [];
            var currentVersion = GetCurrentVersion();
            var newerRelease = releases
                .Where(item => !item.Prerelease)
                .Select(item => new { Source = item, IsVersion = TryParseVersion(item.TagName, out var version), Version = version })
                .Where(item => item.IsVersion && item.Version > currentVersion)
                .OrderByDescending(item => item.Version)
                .FirstOrDefault();

            if (newerRelease is null)
            {
                return UpdateCheckResult.NoUpdate(currentVersion);
            }

            var release = TryCreateRelease(newerRelease.Source);
            if (release is null)
            {
                return UpdateCheckResult.ReleaseUnavailable(
                    $"发现版本 {newerRelease.Version}，但发行版缺少可校验的安装包。请联系发布者补充 CheemsUI-Setup-*.exe 及其 .sha256 文件。");
            }

            return UpdateCheckResult.UpdateAvailable(currentVersion, release);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return UpdateCheckResult.ConnectionFailed("检查更新超时，请稍后重试。");
        }
        catch (HttpRequestException)
        {
            return UpdateCheckResult.ConnectionFailed("无法连接到 Gitee，请检查网络后重试。");
        }
        catch (JsonException)
        {
            return UpdateCheckResult.ConnectionFailed("更新服务返回的数据无法识别，请稍后重试。");
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex);
            return UpdateCheckResult.ConnectionFailed("检查更新时发生异常，详情已写入错误日志。");
        }
    }

    public async Task<string> DownloadInstallerAsync(
        UpdateRelease release,
        IProgress<UpdateDownloadProgress> progress,
        CancellationToken cancellationToken)
    {
        var updateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CheemsUI", "Updates");
        Directory.CreateDirectory(updateDirectory);

        var installerPath = Path.Combine(updateDirectory, release.Installer.Name);
        var partialPath = installerPath + ".partial";
        TryDelete(partialPath);

        try
        {
            await DownloadFileAsync(release.Installer.DownloadUri, partialPath, progress, cancellationToken);
            await VerifyHashAsync(partialPath, release.Checksum.DownloadUri, cancellationToken);

            TryDelete(installerPath);
            File.Move(partialPath, installerPath);
            return installerPath;
        }
        catch
        {
            TryDelete(partialPath);
            throw;
        }
    }

    private static UpdateRelease? TryCreateRelease(GiteeRelease release)
    {
        if (release.Prerelease || !TryParseVersion(release.TagName, out var version))
        {
            return null;
        }

        var installer = release.Assets.FirstOrDefault(asset =>
            asset.Name.StartsWith("CheemsUI-Setup-", StringComparison.OrdinalIgnoreCase) &&
            asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
            Uri.TryCreate(asset.BrowserDownloadUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps);
        if (installer is null || !Uri.TryCreate(installer.BrowserDownloadUrl, UriKind.Absolute, out var installerUri))
        {
            return null;
        }

        var checksum = release.Assets.FirstOrDefault(asset =>
            string.Equals(asset.Name, installer.Name + ".sha256", StringComparison.OrdinalIgnoreCase) &&
            Uri.TryCreate(asset.BrowserDownloadUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps);
        if (checksum is null || !Uri.TryCreate(checksum.BrowserDownloadUrl, UriKind.Absolute, out var checksumUri))
        {
            return null;
        }

        return new UpdateRelease(
            version,
            release.Name ?? release.TagName ?? version.ToString(),
            release.Body ?? string.Empty,
            new UpdateAsset(installer.Name, installerUri),
            new UpdateAsset(checksum.Name, checksumUri));
    }

    private static async Task DownloadFileAsync(
        Uri address,
        string destination,
        IProgress<UpdateDownloadProgress> progress,
        CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        var buffer = new byte[81920];
        long downloadedBytes = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            downloadedBytes += read;
            progress.Report(new UpdateDownloadProgress(downloadedBytes, totalBytes));
        }
    }

    private static async Task VerifyHashAsync(string filePath, Uri checksumUri, CancellationToken cancellationToken)
    {
        var expectedHashText = await HttpClient.GetStringAsync(checksumUri, cancellationToken);
        var expectedHash = expectedHashText
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(token => token.Length == 64 && token.All(Uri.IsHexDigit));
        if (expectedHash is null)
        {
            throw new InvalidDataException("发行版校验文件格式无效，已取消安装。");
        }

        await using var stream = File.OpenRead(filePath);
        var actualHash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
        if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("下载文件的校验值不匹配，已取消安装。");
        }
    }

    private static Version GetCurrentVersion()
    {
        var versionText = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        return TryParseVersion(versionText, out var version) ? version : new Version(0, 0, 0);
    }

    private static bool TryParseVersion(string? value, out Version version)
    {
        var normalized = value?.Trim().TrimStart('v', 'V').Split('+')[0];
        return Version.TryParse(normalized, out version!);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CheemsUI", "1.0"));
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        return client;
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}

internal enum UpdateCheckState { NoUpdate, UpdateAvailable, ReleaseUnavailable, ConnectionFailed }

internal sealed record UpdateCheckResult(UpdateCheckState State, Version? CurrentVersion, UpdateRelease? Release, string? Message)
{
    public static UpdateCheckResult NoUpdate(Version currentVersion) => new(UpdateCheckState.NoUpdate, currentVersion, null, null);
    public static UpdateCheckResult UpdateAvailable(Version currentVersion, UpdateRelease release) => new(UpdateCheckState.UpdateAvailable, currentVersion, release, null);
    public static UpdateCheckResult ReleaseUnavailable(string message) => new(UpdateCheckState.ReleaseUnavailable, null, null, message);
    public static UpdateCheckResult ConnectionFailed(string message) => new(UpdateCheckState.ConnectionFailed, null, null, message);
}

internal sealed record UpdateRelease(Version Version, string Title, string Notes, UpdateAsset Installer, UpdateAsset Checksum);
internal sealed record UpdateAsset(string Name, Uri DownloadUri);
internal readonly record struct UpdateDownloadProgress(long DownloadedBytes, long? TotalBytes)
{
    public double Percentage => TotalBytes is > 0 ? DownloadedBytes * 100d / TotalBytes.Value : 0;
}
