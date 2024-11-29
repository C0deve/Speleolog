namespace SpeleoLog.LogFileViewer.V2;

public class EndToStartPaginatorV2(int pageSize)
{
    private int _itemCount;
    private int LastIndex => Math.Max(0, _itemCount - 1);
    public bool IsOnLastPage => ActualPage.End == LastIndex;
    public bool CanGoNext => !IsOnLastPage;
    public bool CanGoPrevious => ActualPage.Start > 0;

    public PageRange ActualPage { get; private set; } = PageRange.Empty;

    /// <summary>
    ///  Set items count and go to last page
    /// </summary>
    /// <param name="itemCount"></param>
    /// <returns></returns>
    public EndToStartPaginatorV2 Reset(int itemCount)
    {
        _itemCount = itemCount;
        GoToLastPage();
        return this;
    }

    public EndToStartPaginatorV2 Move(int stepSize) =>
        stepSize switch
        {
            > 0 => Next(stepSize),
            < 0 => Previous(stepSize),
            0 => this,
        };

    private EndToStartPaginatorV2 Next(int stepSize)
    {
        if (!CanGoNext) return this;

        var newEnd = ActualPage.End + stepSize;
        var delta = newEnd <= LastIndex ? stepSize : LastIndex - ActualPage.End;
        ActualPage = ActualPage.Move(delta);

        return this;
    }

    private EndToStartPaginatorV2 Previous(int stepSize)
    {
        if (!CanGoPrevious) return this;

        var delta = ActualPage.Start - stepSize >= 0 ? stepSize : ActualPage.Start;
        ActualPage = ActualPage.Move(delta);

        return this;
    }

    public EndToStartPaginatorV2 Push(int itemCount)
    {
        var wasOnLastPage = IsOnLastPage;
        _itemCount += itemCount;

        if (wasOnLastPage) GoToLastPage();
        return this;
    }

    private void GoToLastPage() =>
        ActualPage = PageRange.Create(Math.Max(0, _itemCount - pageSize), _itemCount - 1);
}