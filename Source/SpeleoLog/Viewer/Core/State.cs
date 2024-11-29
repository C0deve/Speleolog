namespace SpeleoLog.Viewer.Core;

public class State
{
    private readonly List<IEvent> _events = [];
    private PageRange _actualPage = PageRange.Empty;
    private readonly Cache _cache = new();
    private readonly Paginator _paginator;
    private string _highlight = string.Empty;

    public IEvent[] Events => _events.ToArray();
    public int TotalLogsCount => _cache.TotalLogsCount;
    public int FilteredLogsCount => _cache.FilteredLogsCount;
    public bool IsSearchOn => _cache.IsSearchOn;

    private State(int pageRange) => _paginator = new Paginator(pageRange);

    public static State Initial(int pageRange) => new(pageRange);

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
                GoNext();
                break;
            case GoToTop:
                DoGoToTop();
                break;
            case Previous:
                GoPrevious();
                break;
            case Refresh refresh:
                DoRefresh(refresh);
                break;
            case SetErrorTag setErrorTag:
                DoSetErrorTag(setErrorTag);
                break;
            case Highlight highlight:
                DoHighlight(highlight);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command));
        }

        return this;
    }

    private void DoHighlight(Highlight command)
    {
        if (command.HighlightText == _highlight) return;
        _highlight = command.HighlightText;
        var blocs = BuildBlocs(_actualPage[..]);
        _events.Add(new Updated(blocs));
    }

    private void DoSetErrorTag(SetErrorTag setErrorTag)
    {
        if (string.Equals(setErrorTag.ErrorTag, _cache.ErrorTag, StringComparison.OrdinalIgnoreCase)) return;

        _cache.ErrorTag = setErrorTag.ErrorTag;
        ResetDisplayedRows();
    }

    private void GoPrevious()
    {
        if (!_paginator.CanGoPrevious) return;
        _paginator.Move(-50);
        _events.AddRange(RefreshDisplayedRows());
    }

    private void GoNext()
    {
        if (!_paginator.CanGoNext) return;
        _paginator.Move(50);
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

    private void DoRefresh(Refresh refresh)
    {
        _cache.Push(refresh.Text);

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
            _events.Add(new DeletedAll());
            _actualPage = PageRange.Empty;
        }

        _paginator.Reset(_cache.FilteredLogsCount);
        _events.AddRange(RefreshDisplayedRows());
    }

    private IEvent[] RefreshDisplayedRows()
    {
        var newPage = _paginator.ActualPage;
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
        var logRows = BuildBlocs(isGoneForward.AddedFomTop);
        if (logRows.Length != 0)
            yield return new AddedToTheTop(isGoneForward.DeleteFromBottom.Length,
                isGoneForward.PageSize,
                _paginator.IsOnLastPage,
                logRows);
    }

    private IEnumerable<IEvent> Map(IsGoneBackward isGoneBackward)
    {
        var logRows = BuildBlocs(isGoneBackward.AddedFomBottom);
        if (logRows.Length != 0)
            yield return new AddedToTheBottom(isGoneBackward.DeleteFromTop.Length, isGoneBackward.PageSize, logRows);
    }

    private DisplayBloc[] BuildBlocs(int[] range) =>
        _cache[range]
            .Reverse()
            .Select(row => row.SplitHighlightBloc(_highlight))
            .SelectMany(GetBlocs)
            .Group()
            .ToArray();

    private static IEnumerable<DisplayBloc> GetBlocs(DisplayRow row)
    {
        DisplayBloc? last = null;
        foreach (var displayBloc in row.Blocs)
        {
            last = displayBloc;
            yield return displayBloc;
        }

        yield return last is null
            ? new DisplayBloc(Environment.NewLine)
            : last with { Text = Environment.NewLine };
    }
}