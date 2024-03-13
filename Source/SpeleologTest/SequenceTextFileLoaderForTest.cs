using SpeleoLogViewer.ViewModels;

namespace SpeleologTest;

public class SequenceTextFileLoaderForTest(params IEnumerable<string>[] texts) : ITextFileLoader
{
    private int _count = -1; 
    
    public Task<IEnumerable<string>> GetTextAsync(string filePath, CancellationToken cancellationToken)
    {
        _count++;
        return Task.FromResult(_count < texts.Length ? texts[_count] : texts.Last());
    }
}