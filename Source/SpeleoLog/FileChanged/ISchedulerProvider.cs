using System.Reactive.Concurrency;

namespace SpeleoLog.FileChanged;

public interface ISchedulerProvider
{
    IScheduler MainThreadScheduler { get; }
    IScheduler TaskpoolScheduler { get; }
}