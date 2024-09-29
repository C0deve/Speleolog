using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;

namespace SpeleoLogViewer.LogFileViewer;

public static class Extensions
{
    public static IObservable<ImmutableArray<LogLinesAggregate>> LogToAggregateStream(
        this IObservable<(IEnumerable<string> Logs, string Mask, string ErrorTag)> input,
        Func<string, IEnumerable<string>, IEnumerable<string>> maskText) =>
        input
            .Select(data => (Logs: maskText(data.Mask, data.Logs), data.ErrorTag))
            .Select(data => LogAggregator.AggregateLog(data.Logs, data.ErrorTag))
            .Where(aggregates => aggregates.Any())
            .Select(aggregates => aggregates.ToImmutableArray());
}