namespace SpeleoLog._BaseClass;

public interface ISchedulerProvider
{
    IScheduler MainThreadScheduler { get; }
    IScheduler TaskpoolScheduler { get; }
}