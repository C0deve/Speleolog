using System.Reactive.Concurrency;

namespace SpeleoLogViewer.FileChanged;

public interface ISchedulerProvider
{
    IScheduler MainThreadScheduler { get; }
    IScheduler TaskpoolScheduler { get; }
}