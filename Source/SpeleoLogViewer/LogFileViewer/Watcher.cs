using System;
using System.Diagnostics;

namespace SpeleoLogViewer.LogFileViewer;

public class Watcher(string libelle) : IDisposable
{
    private readonly Stopwatch _watch = Stopwatch.StartNew();

    public void Dispose()
    {
        _watch.Stop();
        Debug.WriteLine($"{libelle} {_watch.ElapsedMilliseconds}ms");
    }
}