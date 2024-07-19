using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeleoLogViewer.LogFileViewer;

public class Cache(IEnumerable<string> logs)
{
    private readonly List<string> _logs = logs.ToList();

    public string[] Values => _logs.ToArray();
    public int[] AllIndex => _logs.Select((_, index) => index).ToArray();

    public Cache Add(IEnumerable<string> newLines)
    {
        foreach (var newLine in newLines) _logs.Add(newLine);
        return this;
    }

    public IEnumerable<int> Contains(string filter)
    {
        if (_logs.Count == 0) 
            return []; 
        
        if (string.IsNullOrWhiteSpace(filter))
            return AllIndex;

        return _logs
            .Select((line, index) => line.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ? index : -1)
            .Where(index => index > -1);
    }

    public IEnumerable<string> FromIndex(IEnumerable<int> index) => index.Select(i => _logs[i]);
}