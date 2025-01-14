namespace SpeleoLog.Viewer.Core;

/// <summary>
/// Manages the pagination of a list based on the number of items that can be displayed.
/// Displays the last page by default.
/// The first displayed page contains just enough items to fill the available space
/// then it grows to reach <see cref="MaxPageSize"/> to limit memory usage
/// </summary>
public class PaginatorV3
{
    private int _totalCount;
    private int _displayedCount;
    private int Step => _displayedCount / 2;
    private int MaxPageSize => 3 * _displayedCount;
    private int FirstPageSize => Convert.ToInt32(_displayedCount * 1.5);
    private int LastIndex => Math.Max(0, _totalCount - 1);
    public bool IsOnLastPage => CurrentPage.End == LastIndex;
    public bool CanMoveForward => !IsOnLastPage;
    public bool CanMoveBackward => CurrentPage.Start > 0;
    private bool CanExpandsBackwardsCurrentPage => CurrentPage.Size < MaxPageSize;

    /// <summary>
    /// Current displayed index
    /// </summary>
    public PageRange CurrentPage { get; private set; } = PageRange.Empty;

    /// <summary>
    /// Set items count and go to last page
    /// </summary>
    /// <param name="itemCount"></param>
    /// <returns></returns>
    public PaginatorV3 Reset(int itemCount)
    {
        _totalCount = itemCount;
        GoToLastPage();
        return this;
    }

    /// <summary>
    /// Move the <see cref="CurrentPage"/> one step forward if it is not already on the last page
    /// </summary>
    /// <returns></returns>
    public PaginatorV3 MoveForward()
    {
        if (!CanMoveForward) return this;

        var newEnd = CurrentPage.End + Step;
        var delta = newEnd <= LastIndex ? Step : LastIndex - CurrentPage.End;
        CurrentPage = CurrentPage.Move(delta);

        return this;
    }

    /// <summary>
    /// Move the <see cref="CurrentPage"/> one step backward if it is not already on the first page
    /// </summary>
    /// <returns></returns>
    public PaginatorV3 MoveBackward()
    {
        if (!CanMoveBackward) return this;

        CurrentPage = CanExpandsBackwardsCurrentPage
            ? CurrentPage.ExpandsBackward(Step, MaxPageSize)
            : CurrentPage.Move(Step * -1);

        return this;
    }

    /// <summary>
    /// Add <param name="itemCount" /> to the total item count.
    /// Stay on the last page if already on it before the push
    /// </summary>
    /// <param name="itemCount" />
    /// <returns />
    public PaginatorV3 Push(int itemCount)
    {
        var wasOnLastPage = IsOnLastPage;
        _totalCount += itemCount;

        if (wasOnLastPage) GoToLastPage();
        return this;
    }

    private void GoToLastPage() =>
        CurrentPage = FirstPageSize == 0 || _totalCount == 0
            ? PageRange.Empty
            : PageRange.Create(Math.Max(0, _totalCount - FirstPageSize), _totalCount - 1);


    public PaginatorV3 SetTotal(int total)
    {
        _totalCount = total;
        return this;
    }

    public PaginatorV3 SetDisplayedRange(int displayedCount)
    {
        _displayedCount = displayedCount;
        return this;
    }
}