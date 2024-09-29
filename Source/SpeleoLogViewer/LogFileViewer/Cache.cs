using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SpeleoLogViewer.LogFileViewer;

public class Cache
{
    private readonly Subject<int[]> _initialized = new();
    private readonly Subject<string[]> _added = new();
    private readonly List<string> _logs = [];
    private int _actualLength;
    public string[] Values => _logs.ToArray();
    public int[] AllIndex => _logs.Select((_, index) => index).ToArray();
    public IObservable<int[]> Initialized => _initialized.AsObservable();
    public IObservable<string[]> Added => _added.AsObservable();

    private void Add(IEnumerable<string> newLines)
    {
        foreach (var newLine in newLines) _logs.Add(newLine);
    }

    public IEnumerable<int> Contains(string filter)
    {
        if (_logs.Count == 0)
            return [];

        if (string.IsNullOrWhiteSpace(filter))
            return AllIndex;

        return _logs
            .Select((row, index) => row.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ? index : -1)
            .Where(index => index > -1);
    }

    public IEnumerable<string> FromIndex(IEnumerable<int> index) => index.Select(i => _logs[i]);

    public Cache Init(string input)
    {
        _logs.Clear();
        _actualLength = input.Length;
        Add(Split(input));
        _initialized.OnNext(AllIndex);
        return this;
    }
    
    public void Refresh(string input)
    {
        var newValues = Split(Diff(input));
        if(newValues.Length == 0) return;
        Add(newValues);
        _actualLength = input.Length;
        _added.OnNext(newValues);
    }

    private static string[] Split(string actualText)
    {
        using (new Watcher("Split"))
        {
            return actualText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    private string Diff(string input)
    {
        var newTextLength = input.Length;

        return _actualLength <= newTextLength
            ? input.Substring(_actualLength, newTextLength - _actualLength) // file size increases so logs have been added : keep only the changes 
            : Environment.NewLine + input; // file size decreases so it is a creation. keep all text and add an artificial row break
    }
}