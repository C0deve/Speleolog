namespace SpeleoLog.Viewer.Core;

public class Paginator(int pageSize)
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
    public Paginator Reset(int itemCount)
    {
        _itemCount = itemCount;
        GoToLastPage();
        return this;
    }

    public Paginator Move(int stepSize) =>
        stepSize switch
        {
            > 0 => Next(stepSize),
            < 0 => Previous(stepSize),
            0 => this,
        };

    private Paginator Next(int stepSize)
    {
        if (!CanGoNext) return this;

        var newEnd = ActualPage.End + stepSize;
        var delta = newEnd <= LastIndex ? stepSize : LastIndex - ActualPage.End;
        ActualPage = ActualPage.Move(delta);

        return this;
    }

    private Paginator Previous(int stepSize)
    {
        if (!CanGoPrevious) return this;

        var delta = ActualPage.Start - stepSize >= 0 ? stepSize : ActualPage.Start;
        ActualPage = ActualPage.Move(delta);

        return this;
    }

    public Paginator Push(int itemCount)
    {
        var wasOnLastPage = IsOnLastPage;
        _itemCount += itemCount;

        if (wasOnLastPage) GoToLastPage();
        return this;
    }

    private void GoToLastPage() =>
        ActualPage = PageRange.Create(Math.Max(0, _itemCount - pageSize), _itemCount - 1);
}