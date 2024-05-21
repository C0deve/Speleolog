using System.Collections.Generic;
using System.Data;

namespace SpeleoLogViewer.LogFileViewer;

public class Paginator<T>(IEnumerable<T> rows, int pageSize)
{
    private readonly Stack<T> _stack = new(rows);

    public IEnumerable<T> Next()
    {
        if (_stack.Count == 0) yield break;

        var i = 0;
        while (i < pageSize && _stack.TryPop(out var result))
        {
            yield return result ?? throw new NoNullAllowedException();
            i++;
        }
    }

    public bool IsEmpty() => _stack.Count == 0;
}