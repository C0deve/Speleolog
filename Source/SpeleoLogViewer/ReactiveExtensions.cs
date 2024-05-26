using System;
using System.Reactive;
using System.Reactive.Linq;

namespace SpeleoLogViewer;

public static class ReactiveExtensions
{
    public static IObservable<TResult> WhereIs<TResult>(
        this IObservable<object?> source) =>
        Observable.Create<TResult>(o =>
            source.Subscribe(x =>
                {
                    try
                    {
                        if (x is TResult cast)
                            o.OnNext(cast);
                    }
                    catch (Exception ex)
                    {
                        o.OnError(ex);
                    }
                },
                o.OnError,
                o.OnCompleted));

    /// <summary>
    /// Switch to the observable provided by the selector function
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="source"></param>
    /// <param name="selector"></param>
    /// <returns></returns>
    public static IObservable<TResult> Switch<TSource,TResult>(this IObservable<TSource> source, Func<TSource,IObservable<TResult>> selector) =>
        source
            .Select(selector)
            .Switch();

    
    public static IObservable<TResult> IsNotNull<TResult>(
        this IObservable<TResult?> source)
    {
        return Observable.Create<TResult>(o =>
            source.Subscribe(x =>
                {
                    if (x != null)
                        o.OnNext(x);
                },
                o.OnError,
                o.OnCompleted));
    }

    public static IObservable<bool> IsTrue(
        this IObservable<bool> source)
    {
        return Observable.Create<bool>(o =>
            source.Subscribe(x =>
                {
                    if (x)
                        o.OnNext(true);
                },
                o.OnError,
                o.OnCompleted));
    }

    public static IObservable<bool> IsTrue(
        this IObservable<bool?> source)
    {
        return Observable.Create<bool>(o =>
            source.Subscribe(x =>
                {
                    if (x.HasValue && x.Value)
                        o.OnNext(true);
                },
                o.OnError,
                o.OnCompleted));
    }

    public static IObservable<bool> IsFalse(
        this IObservable<bool> source)
    {
        return Observable.Create<bool>(o =>
            source.Subscribe(x =>
                {
                    if (!x)
                        o.OnNext(false);
                },
                o.OnError,
                o.OnCompleted));
    }

    public static IObservable<bool> IsFalse(
        this IObservable<bool?> source)
    {
        return Observable.Create<bool>(o =>
            source.Subscribe(x =>
                {
                    if (x.HasValue && !x.Value)
                        o.OnNext(false);
                },
                o.OnError,
                o.OnCompleted));
    }

    public static IObservable<string> IsNotEmpty(this IObservable<string> source) =>
        Observable.Create<string>(o =>
            source.Subscribe(x =>
                {
                    if (!string.IsNullOrWhiteSpace(x))
                        o.OnNext(x);
                },
                o.OnError,
                o.OnCompleted));

    public static IObservable<string> IsEmpty(this IObservable<string> source) =>
        Observable.Create<string>(o =>
            source.Subscribe(x =>
                {
                    if (string.IsNullOrWhiteSpace(x))
                        o.OnNext(x);
                },
                o.OnError,
                o.OnCompleted));

    public static IObservable<Unit> ToUnit<TSource>(this IObservable<TSource> source) => 
        source.Select(_ => Unit.Default);
}