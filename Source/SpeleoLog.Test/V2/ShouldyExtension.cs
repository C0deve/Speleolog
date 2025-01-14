using Shouldly;
using SpeleoLog.Viewer.Core;

namespace SpeleoLog.Test.V2;

internal static class ShouldyExtension
{
    public static void ShouldBe(this IEvent[] events, IEnumerable<IEvent> expected)
    {
        var array = expected.ToArray();
        events.Length.ShouldBe(array.Length);
        events.ShouldSatisfyAllConditions(() =>
        {
            for (var i = 0; i < events.Length; i++)
            {
                var actual = events[i];
                var theExpected = array[i];
                actual.GetType().ShouldBe(theExpected.GetType());
               
                switch (actual)
                {
                    case AddedToTheTop addedToTheTop:
                        var theExpected1 = (AddedToTheTop)theExpected;
                        addedToTheTop.IsOnTop.ShouldBe(theExpected1.IsOnTop);
                        addedToTheTop.RemovedFromBottomCount.ShouldBe(theExpected1.RemovedFromBottomCount);
                        addedToTheTop.Rows.ToArray().ShouldBe( theExpected1.Rows.ToArray());
                        break;
                    case AddedToTheBottom addedToTheBottom:
                        var theExpected2 = (AddedToTheBottom)theExpected;
                        addedToTheBottom.Rows.ToArray().ShouldBe(theExpected2.Rows.ToArray());
                        break;
                    case AllReplaced updated:
                        updated.Rows.ToArray().ShouldBe(((AllReplaced)theExpected).Rows.ToArray());
                        break;
                    default:
                        actual.ShouldBe(theExpected);
                        break;
                }
            }
        });
    }

    private static void ShouldBe(this DisplayedRow[] actualLogList, DisplayedRow[] expectedLogList)
    {
        for (var i1 = 0; i1 < actualLogList.Length; i1++) 
            actualLogList[i1].ShouldBe(expectedLogList[i1]);
    }
    private static void ShouldBe(this DisplayedRow actualLogList, DisplayedRow expectedLogList)
    {
        actualLogList.Index.ShouldBe(expectedLogList.Index);
        for (var i1 = 0; i1 < actualLogList.Blocs.Length; i1++) 
            actualLogList.Blocs[i1].ShouldBe(expectedLogList.Blocs[i1], $"i1 = {i1}");
    }
}