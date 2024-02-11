using System.Reactive.Concurrency;

namespace SpeleoLogViewer.Service;

public interface ISchedulerProvider
{
    IScheduler CurrentThread { get; }
    IScheduler Default { get; }
}