using SpeleoLogViewer.LogFileViewer;

namespace SpeleologTest;

public class TextFileLoaderWithDuration(TimeSpan loadingDuration) : ITextFileLoader
{
    public async Task<string> GetTextAsync(string filePath, CancellationToken cancellationToken)
    {
        await Task.Delay(loadingDuration, cancellationToken);
        return "A";
    }
}