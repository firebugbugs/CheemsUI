using CheemsControl.App.Infrastructure;

namespace CheemsControl.App.ViewModels;

public interface ISearchablePageViewModel
{
    bool HasMatches { get; }
    void ApplySearch(string query, bool showAll);
}

/// <summary>
/// 控件页统一搜索基类。每个控件只登记一次搜索元数据，页面只负责绑定可见性。
/// </summary>
public abstract class SearchablePageViewModel : BaseViewModel, ISearchablePageViewModel
{
    private readonly IReadOnlyDictionary<string, string> _searchIndex;
    private HashSet<string> _visibleKeys;

    protected SearchablePageViewModel(IReadOnlyDictionary<string, string> searchIndex)
    {
        _searchIndex = searchIndex;
        _visibleKeys = searchIndex.Keys.ToHashSet(StringComparer.Ordinal);
    }

    public bool HasMatches => _visibleKeys.Count > 0;

    public void ApplySearch(string query, bool showAll)
    {
        var next = showAll || string.IsNullOrWhiteSpace(query)
            ? _searchIndex.Keys.ToHashSet(StringComparer.Ordinal)
            : _searchIndex
                .Where(pair => Matches(query, pair.Value))
                .Select(pair => pair.Key)
                .ToHashSet(StringComparer.Ordinal);

        if (_visibleKeys.SetEquals(next)) return;
        _visibleKeys = next;
        OnPropertyChanged(nameof(HasMatches));
        OnSearchFilterChanged();
    }

    protected bool IsControlVisible(string key) => _visibleKeys.Contains(key);

    protected abstract void OnSearchFilterChanged();

    public static bool Matches(string query, string searchableText)
    {
        var tokens = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.Length > 0 && tokens.All(token =>
            searchableText.Contains(token, StringComparison.OrdinalIgnoreCase));
    }
}
