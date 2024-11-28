using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.LogFileViewer;

public class Sequencer<TOutput>(Action<Exception>? onException = null)
{
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly ConcurrentDictionary<Guid, Task> _tasks = new();
    private readonly Subject<TOutput> _output = new();

    public IObservable<TOutput> Output => _output.AsObservable();
    public Task WaitAll() => Task.WhenAll(_tasks.Values);

    public Sequencer<TOutput> Enqueue(Func<TOutput> action)
    {
        var id = Guid.NewGuid();
        var task = Task.Run(() =>
        {
            _semaphore.Wait();
            try
            {
                _output.OnNext(action());
            }
            catch (Exception e)
            {
                if (onException is null) throw;
                onException?.Invoke(e);
            }
            finally
            {
                _tasks.TryRemove(id, out _);
                _semaphore.Release();
            }
        });

        _tasks.TryAdd(id, task);

        return this;
    }
}