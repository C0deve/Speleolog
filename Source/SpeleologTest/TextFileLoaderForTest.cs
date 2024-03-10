using SpeleoLogViewer.ViewModels;

namespace SpeleologTest;

public class TextFileLoaderForTest(Func<object, object, Task<IEnumerable<string>>> getTextAsync) : ITextFileLoader
{
    public Task<IEnumerable<string>> GetTextAsync(string filePath, CancellationToken cancellationToken) => getTextAsync(filePath, cancellationToken);
}