namespace SpeleoLog.Viewer.Core;

public class Cache
{
    private readonly List<string> _logs = [];
    private readonly List<int> _filteredIndex = [];
    private int _refreshCount;

    public bool IsSearchOn => !string.IsNullOrWhiteSpace(SearchTerm);
    public int FilteredLogsCount => _filteredIndex.Count;
    public bool IsInitialized => _refreshCount > 1;
    public List<int> LastAddedIndex { get; } = [];
    public int TotalLogsCount => _logs.Count;
    public string Mask { get; private set; } = string.Empty;
    public string SearchTerm { get; private set; } = string.Empty;
    public string ErrorTag { get; set; } = string.Empty;

    public Cache SetSearchTerm(string text)
    {
        SearchTerm = text;
        _filteredIndex.Clear();
        _filteredIndex.AddRange(GetAllIndex(SearchTerm));
        return this;
    }

    public Cache SetMask(string mask)
    {
        Mask = mask;
        return this;
    }
    
    public Row[] this[IEnumerable<int> range] =>
        range
            .Where(i => i < _filteredIndex.Count)
            .Select(i => _filteredIndex[i])
            .Select(x => new Row(x, _logs[x], IsNewLine: IsInitialized && LastAddedIndex.Contains(x)))
            .SetIsError(ErrorTag)
            .MaskRows(Mask)
            .ToArray();

    public Row[] this[Range range] =>
        _filteredIndex[range]
            .Select(x => new Row(x, _logs[x], IsNewLine: IsInitialized && LastAddedIndex.Contains(x)))
            .SetIsError(ErrorTag)
            .MaskRows(Mask)
            .ToArray();
    
    public Cache Push(params string[] input)
    {
        LastAddedIndex.Clear();
        Add(input);
        _refreshCount++;

        return this;
    }

    private void Add(params string[] newRows)
    {
        foreach (var newRow in newRows)
        {
            var index = Add(newRow);

            if (!Contains(SearchTerm, newRow)) continue;

            _filteredIndex.Add(index);
            LastAddedIndex.Add(index);
        }
    }

    private int Add(string newRow)
    {
        _logs.Add(newRow);
        return _logs.Count - 1;
    }

    /// <summary>
    /// Return all index (matching search if provided) starting from startIndex 
    /// </summary>
    /// <param name="search"></param>
    /// <param name="startIndex"></param>
    /// <returns></returns>
    private IEnumerable<int> GetAllIndex(string search, int startIndex = 0)
    {
        if (_logs.Count == 0) return [];

        return IsSearchOn
            ? _logs
                .Select((row, index) => (row, index))
                .Skip(startIndex)
                .Where(x => Contains(search, x.row))
                .Select(x => x.index)
            : _logs
                .Select((_, index) => index)
                .Skip(startIndex);
    }

    private static bool Contains(string search, string row) =>
        row.Contains(search, StringComparison.InvariantCultureIgnoreCase);

    public void ClearLastAdded() => LastAddedIndex.Clear();
}