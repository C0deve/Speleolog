using SpeleoLogViewer.ViewModels;

namespace SpeleologTest;

public class EmptyFileLoader: ITextFileLoader
{
    public Task<IEnumerable<string>> GetTextAsync(string filePath, CancellationToken cancellationToken) => 
        Task.FromResult<IEnumerable<string>>([]);
}