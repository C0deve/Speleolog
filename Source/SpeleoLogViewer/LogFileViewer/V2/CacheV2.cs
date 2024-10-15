using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeleoLogViewer.LogFileViewer.V2;

public class CacheV2
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
    public string ErrorTag { get; private set; } = string.Empty;

    public CacheV2 SetSearchTerm(string text)
    {
        SearchTerm = text;
        _filteredIndex.Clear();
        _filteredIndex.AddRange(GetAllIndex(SearchTerm));
        return this;
    }

    public CacheV2 SetMask(string mask)
    {
        Mask = mask;
        return this;
    }
    
    public CacheV2 SetErrorTag(string errorTag)
    {
        ErrorTag = errorTag;
        return this;
    }

    public LogLine[] this[IEnumerable<int> range] =>
        range
            .Where(i => i < _filteredIndex.Count)
            .Select(i => _filteredIndex[i])
            .Select(x => new LogLine(_logs[x], IsInitialized && LastAddedIndex.Contains(x)))
            .UpdateIsError(ErrorTag)
            .MaskRows(Mask)
            .ToArray();

    public LogLine[] this[Range range] =>
        _filteredIndex[range]
            .Select(x => new LogLine(_logs[x], IsInitialized && LastAddedIndex.Contains(x)))
            .UpdateIsError(ErrorTag)
            .MaskRows(Mask)
            .ToArray();
    
    public CacheV2 Push(params string[] input)
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

internal static class Extensions
{
    public static IEnumerable<LogLine> MaskRows(this IEnumerable<LogLine> actualRows, string mask) =>
        string.IsNullOrWhiteSpace(mask)
            ? actualRows
            : actualRows.Select(x => Mask(x, mask));

    private static LogLine Mask(LogLine row, string mask)
    {
        var index = row.Text.IndexOf(mask, StringComparison.Ordinal);
        var maskedRow = index < 0
            ? row
            : row with { Text = row.Text.Remove(index, mask.Length) };
        return maskedRow;
    }
    
    public static IEnumerable<LogLine> UpdateIsError(this IEnumerable<LogLine> actualRows, string errorTag) =>
        string.IsNullOrWhiteSpace(errorTag)
            ? actualRows
            : actualRows.Select(x => UpdateIsError(x, errorTag));

    private static LogLine UpdateIsError(LogLine row, string errorTag) =>
        row.Text.Contains(errorTag, StringComparison.InvariantCultureIgnoreCase)
        ? row with { IsError = true }
        : row;
}