using System;
using System.Diagnostics;

namespace SpeleoLogViewer.LogFileViewer;

internal class Watcher(string libelle) : IDisposable
{
    private readonly Stopwatch _watch = Stopwatch.StartNew();

    public void Dispose()
    {
        _watch.Stop();
        Console.WriteLine($"{libelle} {_watch.ElapsedMilliseconds}ms");
    }
}