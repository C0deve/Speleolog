using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeleoLogViewer.LogFileViewer.V2;

public class State : ValueObject
{
    private static string _errorTag = "";
    private readonly List<IEvent> _events = [];
    private PageRange _actualPage = PageRange.Empty;
    private readonly CacheV2 _cache = new();
    private readonly EndToStartPaginatorV2 _paginator;

    public IEvent[] Events => _events.ToArray();

    private State(int pageRange) => _paginator = new EndToStartPaginatorV2(pageRange);

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
            case Previous:
                GoPrevious();
                break;
            case Refresh refresh:
                DoRefresh(refresh);
                break;
            case SetErrorTag setErrorTag:
                DoSetErrorTag(setErrorTag);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command));
        }

        return this;
    }

    private void DoSetErrorTag(SetErrorTag setErrorTag)
    {
        if (string.Equals(setErrorTag.ErrorTag, _cache.ErrorTag, StringComparison.OrdinalIgnoreCase)) return;

        _cache.SetErrorTag(setErrorTag.ErrorTag);
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
        var evts = _actualPage.Compare(newPage) switch
        {
            IsGoneBackward isGoneBackward => Map(isGoneBackward),
            IsGoneForward isGoneForward => Map(isGoneForward),
            IsUnchanged => [],
            _ => throw new ArgumentOutOfRangeException()
        };
        _actualPage = newPage;
        return evts.ToArray();
    }

    private IEnumerable<IEvent> Map(IsGoneForward isGoneForward)
    {
        var logRows = _cache[isGoneForward.AddedFomTop].ToArray();
        if (logRows.Length != 0)
            yield return new AddedToTheTop(logRows);

        if (isGoneForward.DeleteFromBottom.Length > 0)
            yield return new DeletedFromBottom(isGoneForward.DeleteFromBottom.Length);
    }

    private IEnumerable<IEvent> Map(IsGoneBackward isGoneBackward)
    {
        var logRows = _cache[isGoneBackward.AddedFomBottom].ToArray();
        if (logRows.Length != 0)
            yield return new AddedToTheBottom(logRows);

        if (isGoneBackward.DeleteFromTop.Length > 0)
            yield return new DeletedFromTop(isGoneBackward.DeleteFromTop.Length);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        foreach (var index in _actualPage)
            yield return index;

        yield return _cache;
    }
}