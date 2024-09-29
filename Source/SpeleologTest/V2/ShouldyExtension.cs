using Shouldly;
using SpeleoLogViewer.LogFileViewer.V2;

namespace SpeleologTest.V2;

internal static class ShouldyExtension
{
    public static void ShouldBe(this LogGroup[] source, IEnumerable<LogGroup> expected)
    {
        var array = expected.ToArray();
        source.Length.ShouldBe(array.Length);
        source.ShouldSatisfyAllConditions(() =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var actual = source[i];
                var theExpected = array[i];
                actual.GetType().ShouldBe(theExpected.GetType());
                actual.Rows.ShouldBe(theExpected.Rows);
            }
        });
    }

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
                    case EventBase @event:
                        var actualLogList = @event.Rows.ToArray();
                        var expectedLogList = ((EventBase)theExpected).Rows.ToArray();
                        
                        for (var i1 = 0; i1 < @event.Rows.Count; i1++) 
                            actualLogList[i1].ShouldBe(expectedLogList[i1], $"i1 = {i1}");
                        
                        break;
                    default:
                        actual.ShouldBe(theExpected);
                        break;
                }
            }
        });
    }
}