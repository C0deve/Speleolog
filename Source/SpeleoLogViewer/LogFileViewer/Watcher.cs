using System;
using System.Diagnostics;

namespace SpeleoLogViewer.LogFileViewer;

internal class Watcher : IDisposable
{
    private readonly string _libelle;
    private readonly Stopwatch _watch = Stopwatch.StartNew();

    public Watcher(string libelle)
    {
        _libelle = libelle;
    }

    public void Dispose()
    {
        _watch.Stop();
        Console.WriteLine($"{_libelle} {_watch.ElapsedMilliseconds}ms");
    }
}