using System.Collections.Generic;

namespace SpeleoLogViewer.LogFileViewer;

public static class Extensions
{
    public static string Join(this IEnumerable<string> input, string separator) => string.Join(separator, input);
}