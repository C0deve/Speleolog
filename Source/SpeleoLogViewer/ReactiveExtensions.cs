using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace SpeleoLogViewer;

public static class ReactiveExtensions
{
    public static IObservable<TResult> Is<TResult>(
        this IObservable<object?> source) =>
        source
            .IsNotNull()
            .Where(x => x is TResult)
            .Select(x => (TResult)x);

    /// <summary>
    /// Switch to the observable provided by the selector function
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="source"></param>
    /// <param name="selector"></param>
    /// <returns></returns>
    public static IObservable<TResult> Switch<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> selector) =>
        source
            .Select(selector)
            .Switch();
    
    public static IObservable<TResult> SelectAsync<TSource, TResult>(this IObservable<TSource> source, Func<TSource, Task<TResult>> selector) =>
        source
            .Select(x => Observable.FromAsync(() => selector(x)))
            .Switch();


    public static IObservable<TResult> IsNotNull<TResult>(
        this IObservable<TResult?> source) =>
        source
            .Where(x => x is not null)
            .Select(x => x!);

    public static IObservable<bool> IsTrue(
        this IObservable<bool> source) =>
        source
            .Where(x => x)
            .Select(_ => true);

    public static IObservable<bool> IsTrue(this IObservable<bool?> source) =>
        source
            .Where(x => x.HasValue && x.Value)
            .Select(_ => false);

    public static IObservable<bool> IsFalse(this IObservable<bool> source) =>
        source
            .Where(x => !x)
            .Select(_ => false);

    public static IObservable<bool> IsFalse(this IObservable<bool?> source) =>
        source
            .Where(x => x.HasValue && !x.Value)
            .Select(_ => false);

    public static IObservable<string> IsNotEmpty(this IObservable<string> source) =>
        source
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x);

    public static IObservable<string> IsEmpty(this IObservable<string> source) =>
        source
            .Where(string.IsNullOrWhiteSpace)
            .Select(x => x);

    public static IObservable<Unit> ToUnit<TSource>(this IObservable<TSource> source) =>
        source.Select(_ => Unit.Default);
}