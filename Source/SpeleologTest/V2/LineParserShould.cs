using Shouldly;
using SpeleoLogViewer.LogFileViewer.V2;
using Environment = System.Environment;

namespace SpeleologTest.V2;

public class LineParserShould
{
    public class FromEnd
    {
        [Fact]
        public void ReturnStartIndexFromEnd() =>
            string
                .Concat(Enumerable.Repeat("l" + Environment.NewLine, 7))
                .GetIndexOfNthLineFromEnd(2)
                .ShouldBe(new RowInfo(15, 2));

        [Fact]
        public void Return0IfNotEnoughLineFromEnd() =>
            string
                .Concat(Enumerable.Repeat("l" + Environment.NewLine, 3))
                .GetIndexOfNthLineFromEnd(4)
                .ShouldBe(new RowInfo(0, 3));

        [Fact]
        public void ReturnStartIndexFromEndOneLine() =>
            BuildString(0, 1)
                .GetIndexOfNthLineFromEnd(1)
                .ShouldBe(new RowInfo(0, 1));
        
        [Fact]
        public void RemoveLinesFromBottom() =>
            BuildString(0, 30)
                .RemoveNthLineFromBottom(4)
                .ShouldBe(new LineResult(BuildString(0, 26), 4));
        
        [Fact]
        public void RemoveAllLinesFromBottom() =>
            BuildString(0, 30)
                .RemoveNthLineFromBottom(30)
                .ShouldBe(new LineResult(string.Empty, 30));
    }

    public class FromTop
    {
        [Fact]
        public void ReturnEndOfNthLine() =>
            BuildString(0, 9)
                .GetEndIndexOfNthLine(3)
                .ShouldBe(new RowInfo(8, 3));

        [Fact]
        public void ReturnEndOfNthLineIfNotEnoughLine() =>
            BuildString(0, 3)
                .GetEndIndexOfNthLine(4)
                .ShouldBe(new RowInfo(8, 3));

        [Fact]
        public void RemoveAllLinesFromTop() =>
            BuildString(0, 30)
                .RemoveNthLineFromTop(30)
                .ShouldBe(new LineResult(string.Empty, 30));

        [Fact]
        public void RemoveLinesFromTop() =>
            BuildString(0, 30)
                .RemoveNthLineFromTop(4)
                .ShouldBe(new LineResult(BuildString(4, 26), 4));
    }

    private static string BuildString(int start, int count) => string.Concat(Enumerable.Range(start, count).Select(i => $"{i}" + Environment.NewLine));
}