using System.Reactive.Concurrency;
using SpeleoLog._BaseClass;

namespace SpeleoLog.Test;

public class TestSchedulerProvider(IScheduler scheduler) : ISchedulerProvider
{
    public IScheduler MainThreadScheduler => scheduler;
    public IScheduler TaskpoolScheduler  => scheduler;
}