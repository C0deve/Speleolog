namespace SpeleoLog._BaseClass;

public class SchedulerProvider : ISchedulerProvider
{
    public IScheduler MainThreadScheduler => RxApp.MainThreadScheduler;
    public IScheduler TaskpoolScheduler => RxApp.TaskpoolScheduler;
}