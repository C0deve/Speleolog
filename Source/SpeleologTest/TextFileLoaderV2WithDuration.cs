using SpeleoLogViewer.LogFileViewer.V2;

namespace SpeleologTest;

public class TextFileLoaderV2WithDuration(TimeSpan loadingDuration) : ITextFileLoaderV2
{
    public async Task<string[]> GetTextAsync(string filePath, CancellationToken cancellationToken)
    {
        await Task.Delay(loadingDuration, cancellationToken);
        return ["A"];
    }
}