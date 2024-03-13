using SpeleoLogViewer.ViewModels;

namespace SpeleologTest;

public class TextFileLoaderForTest(Func<string, IEnumerable<string>> getText) : ITextFileLoader
{
    public TextFileLoaderForTest(IEnumerable<string> text) : this(_ => text) { }
    
    public Task<IEnumerable<string>> GetTextAsync(string filePath, CancellationToken cancellationToken) => 
        Task.FromResult(getText(filePath));
}