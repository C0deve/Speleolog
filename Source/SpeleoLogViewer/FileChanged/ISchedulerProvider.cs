using System.Reactive.Concurrency;

namespace SpeleoLogViewer.FileChanged;

public interface ISchedulerProvider
{
    IScheduler CurrentThread { get; }
    IScheduler Default { get; }
}