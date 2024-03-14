using SpeleoLogViewer.LogFileViewer;

namespace SpeleologTest;

public class EmptyFileLoader: ITextFileLoader
{
    public Task<string> GetTextAsync(string filePath, CancellationToken cancellationToken) => 
        Task.FromResult(string.Empty);
}