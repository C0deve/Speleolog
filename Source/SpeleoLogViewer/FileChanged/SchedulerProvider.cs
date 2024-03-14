using System.Reactive.Concurrency;

namespace SpeleoLogViewer.FileChanged;

public class SchedulerProvider : ISchedulerProvider
{
    public IScheduler CurrentThread => Scheduler.CurrentThread;
    public IScheduler Default => Scheduler.Default;
}