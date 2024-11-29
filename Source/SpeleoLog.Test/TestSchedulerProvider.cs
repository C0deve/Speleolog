using System.Reactive.Concurrency;
using SpeleoLog.FileChanged;

namespace SpeleoLog.Test;

public class TestSchedulerProvider(IScheduler scheduler) : ISchedulerProvider
{
    public IScheduler MainThreadScheduler => scheduler;
    public IScheduler TaskpoolScheduler  => scheduler;
}