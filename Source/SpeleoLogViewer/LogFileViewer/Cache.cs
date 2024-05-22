using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeleoLogViewer.LogFileViewer;

public class Cache
{
    private readonly Dictionary<int,string> _logs;
    public Cache(IEnumerable<string> logs) =>
        _logs = logs
            .Select((line, index) => (Line: line, Index: index))
            .ToDictionary(x => x.Index, x => x.Line);

    public string[] Values => _logs.Values.ToArray();

    public Cache Add(IEnumerable<string> newLines)
    {
        foreach (var line in  newLines) _logs.Add(_logs.Count, line);
        return this;
    }

    public IEnumerable<int> Contains(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return _logs.Keys;
        
        return _logs
            .Where(pair => pair.Value.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
            .Select(pair => pair.Key);
    }

    public IEnumerable<string> FromIndex(IEnumerable<int> index) => _logs.GetAll(index);
}