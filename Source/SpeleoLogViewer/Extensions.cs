using System.Collections.Generic;
using System.Linq;

namespace SpeleoLogViewer;

public static class Extensions
{
    public static IEnumerable<T> GetAll<T>(this IDictionary<int, T> source, IEnumerable<int> indexes) => 
        indexes.Select(i => source[i]);

    public static IEnumerable<T> GetAll<T>(this IDictionary<int, T> source, params int[] indexes) => 
        GetAll(source, indexes.AsEnumerable());
}