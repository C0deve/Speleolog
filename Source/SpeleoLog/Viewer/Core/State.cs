namespace SpeleoLog.Viewer.Core;

public class State
{
    private readonly List<IEvent> _events = [];
    private PageRange _actualPage = PageRange.Empty;
    private readonly Cache _cache = new();
    private readonly PaginatorV3 _paginator = new();
    private string _highlight = string.Empty;

    public IEvent[] Events => _events.ToArray();
    public int TotalLogsCount => _cache.TotalLogsCount;
    public int FilteredLogsCount => _cache.FilteredLogsCount;
    public bool IsSearchOn => _cache.IsSearchOn;

    public State ClearEvents()
    {
        _events.Clear();
        _cache.ClearLastAdded();
        return this;
    }

    public State Handle(ICommand command)
    {
        switch (command)
        {
            case Filter filter:
                DoFilter(filter);
                break;
            case Mask mask:
                DoMask(mask);
                break;
            case Next:
                MoveForward();
                break;
            case GoToTop:
                DoGoToTop();
                break;
            case Previous:
                MoveBackward();
                break;
            case AddRows refresh:
                DoAddRows(refresh);
                break;
            case SetErrorTag setErrorTag:
                DoSetErrorTag(setErrorTag);
                break;
            case Highlight highlight:
                DoHighlight(highlight);
                break;
            case SetDisplayedRange setPageRange:
                DoSetDisplayedRange(setPageRange);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command));
        }

        return this;
    }

    private void DoSetDisplayedRange(SetDisplayedRange setDisplayedRange)
    {
        _paginator.SetDisplayedRange(setDisplayedRange.PageRange);
        ResetDisplayedRows();
    }

    private void DoHighlight(Highlight command)
    {
        if (command.HighlightText == _highlight) return;
        _highlight = command.HighlightText;
        var rows = BuildDisplayedRows(_actualPage[..]);
        _events.Add(new AllReplaced(rows));
    }

    private void DoSetErrorTag(SetErrorTag setErrorTag)
    {
        if (string.Equals(setErrorTag.ErrorTag, _cache.ErrorTag, StringComparison.OrdinalIgnoreCase)) return;

        _cache.ErrorTag = setErrorTag.ErrorTag;
        ResetDisplayedRows();
    }

    private void MoveBackward()
    {
        if (!_paginator.CanMoveBackward) return;
        _paginator.MoveBackward();
        _events.AddRange(RefreshDisplayedRows());
    }

    private void MoveForward()
    {
        if (!_paginator.CanMoveForward) return;
        _paginator.MoveForward();
        _events.AddRange(RefreshDisplayedRows());
    }

    private void DoGoToTop() => ResetDisplayedRows();

    private void DoMask(Mask mask)
    {
        if (string.Equals(mask.Text, _cache.Mask, StringComparison.OrdinalIgnoreCase)) return;
        _cache.SetMask(mask.Text);
        ResetDisplayedRows();
    }

    private void DoFilter(Filter filter)
    {
        if (string.Equals(filter.Text, _cache.SearchTerm, StringComparison.OrdinalIgnoreCase)) return;
        _cache.SetSearchTerm(filter.Text);
        ResetDisplayedRows();
    }

    private void DoAddRows(AddRows command)
    {
        _cache.Push(command.Rows);

        if (_cache.IsInitialized)
            _paginator.Push(_cache.LastAddedIndex.Count);
        else
            _paginator.Reset(_cache.FilteredLogsCount);

        _events.AddRange(RefreshDisplayedRows());
    }

    private void ResetDisplayedRows()
    {
        if (_actualPage.Size > 0)
        {
            _events.Add(new AllDeleted());
            _actualPage = PageRange.Empty;
        }

        _paginator.Reset(_cache.FilteredLogsCount);
        _events.AddRange(RefreshDisplayedRows());
    }

    private IEvent[] RefreshDisplayedRows()
    {
        var newPage = _paginator.CurrentPage;
        var events = _actualPage.Compare(newPage) switch
        {
            IsGoneBackward isGoneBackward => Map(isGoneBackward),
            IsGoneForward isGoneForward => Map(isGoneForward),
            IsUnchanged => [],
            _ => throw new ArgumentOutOfRangeException()
        };
        _actualPage = newPage;
        return events.ToArray();
    }

    private IEnumerable<IEvent> Map(IsGoneForward isGoneForward)
    {
        var logRows = BuildDisplayedRows(isGoneForward.AddedFomTop);
        if (logRows.Length != 0)
            yield return new AddedToTheTop(isGoneForward.DeleteFromBottom.Length,
                _paginator.IsOnLastPage,
                logRows);
    }

    private IEnumerable<IEvent> Map(IsGoneBackward isGoneBackward)
    {
        var logRows = BuildDisplayedRows(isGoneBackward.AddedFomBottom);
        if (logRows.Length != 0)
            yield return new AddedToTheBottom(isGoneBackward.DeleteFromTop.Length, logRows);
    }

    private DisplayedRow[] BuildDisplayedRows(int[] range) =>
        _cache[range]
            .Reverse()
            .Select(row => row.SplitHighlightBloc(_highlight))
            .ToArray();

    private static IEnumerable<TextBlock> GetBlocs(DisplayedRow row)
    {
        yield return new TextBlock(row.Index
            .ToString()
            .PadRight(8, ' '), IsRowNumber: true);

        TextBlock? last = null;
        foreach (var displayBloc in row.Blocs)
        {
            last = displayBloc;
            yield return displayBloc;
        }

        yield return last is null
            ? new TextBlock(Environment.NewLine)
            : last with { Text = Environment.NewLine };
    }
}