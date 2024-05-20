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

    public Cache Add(string[] newLines)
    {
        foreach (var line in  newLines) _logs.Add(_logs.Count, line);
        return this;
    }
}