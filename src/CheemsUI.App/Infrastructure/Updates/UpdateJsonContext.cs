using System.Text.Json.Serialization;

namespace CheemsUI.App.Infrastructure.Updates;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(List<GiteeRelease>))]
internal partial class UpdateJsonContext : JsonSerializerContext;

internal sealed class GiteeRelease
{
    [JsonPropertyName("tag_name")] public string? TagName { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("body")] public string? Body { get; init; }
    [JsonPropertyName("prerelease")] public bool Prerelease { get; init; }
    [JsonPropertyName("assets")] public List<GiteeReleaseAsset> Assets { get; init; } = [];
}

internal sealed class GiteeReleaseAsset
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("browser_download_url")] public string? BrowserDownloadUrl { get; init; }
}
