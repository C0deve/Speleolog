﻿using System.Reactive.Concurrency;
using ReactiveUI;

namespace SpeleoLogViewer.FileChanged;

public class SchedulerProvider : ISchedulerProvider
{
    public IScheduler MainThreadScheduler => RxApp.MainThreadScheduler;
    public IScheduler TaskpoolScheduler => RxApp.TaskpoolScheduler;
}