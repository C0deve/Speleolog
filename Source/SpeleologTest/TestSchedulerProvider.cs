using System.Reactive.Concurrency;
using SpeleoLogViewer.FileChanged;

namespace SpeleologTest;

public class TestSchedulerProvider(IScheduler scheduler) : ISchedulerProvider
{
    public IScheduler MainThreadScheduler => scheduler;
    public IScheduler TaskpoolScheduler  => scheduler;
}