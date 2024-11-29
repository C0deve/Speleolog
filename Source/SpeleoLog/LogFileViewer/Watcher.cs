using System.Diagnostics;

namespace SpeleoLog.LogFileViewer;

public class Watcher(string libelle) : IDisposable
{
    private readonly Stopwatch _watch = Stopwatch.StartNew();

    public void Dispose()
    {
        _watch.Stop();
        Debug.WriteLine($"{libelle} {_watch.ElapsedMilliseconds}ms");
    }
}