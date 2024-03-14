using SpeleoLogViewer.LogFileViewer;

namespace SpeleologTest;

public class SequenceTextFileLoaderForTest(params IEnumerable<string>[] texts) : ITextFileLoader
{
    private int _count = -1; 
    
    public Task<string> GetTextAsync(string filePath, CancellationToken cancellationToken)
    {
        _count++;
        return Task.FromResult(
            string.Join(Environment.NewLine,
            _count < texts.Length ? texts[_count] : texts.Last()));
    }
}