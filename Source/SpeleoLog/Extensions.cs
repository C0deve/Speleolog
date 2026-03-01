namespace SpeleoLog;

public static class Extensions
{
    public static IObservable<TResult> IsNotNull<TResult>(
        this IObservable<TResult?> source) =>
        source
            .Where(x => x is not null)
            .Select(x => x!);
    
    public static IObservable<TResult> Is<TResult>(
        this IObservable<object?> source) =>
        source
            .IsNotNull()
            .Where(x => x is TResult)
            .Select(x => (TResult)x);

    /// <param name="source"></param>
    /// <typeparam name="TSource"></typeparam>
    extension<TSource>(IObservable<TSource> source)
    {
        /// <summary>
        /// Switch to the observable provided by the selector function
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        /// <returns></returns>
        public IObservable<TResult> Switch<TResult>(Func<TSource, IObservable<TResult>> selector) =>
            source
                .Select(selector)
                .Switch();

        public IObservable<TResult> SelectAsync<TResult>(Func<TSource, Task<TResult>> selector) =>
            source
                .Select(x => Observable.FromAsync(_ => selector(x)))
                .Concat();

        public IObservable<Unit> SelectAsync(Func<TSource, CancellationToken, Task> selector) =>
            source
                .Select(x => Observable.FromAsync(token => selector(x, token)))
                .Switch();

        public IObservable<Unit> ToUnit() =>
            source.Select(_ => Unit.Default);
    }


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

    extension(IObservable<string> source)
    {
        public IObservable<string> IsNotEmpty() =>
            source
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x);

        public IObservable<string> IsEmpty() =>
            source
                .Where(string.IsNullOrWhiteSpace)
                .Select(x => x);
    }

    public static IEnumerable<TResult> IsNotNull<TResult>(
        this IEnumerable<TResult?> source) =>
        source
            .Where(x => x is not null)
            .Select(x => x!);
}