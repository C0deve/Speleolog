using SpeleoLog.Viewer.Core;

namespace SpeleoLog.Test;

public class FileLoaderWithDuration(TimeSpan loadingDuration) : IFileLoader
{
    public async Task<string[]> GetTextAsync(string filePath, CancellationToken cancellationToken)
    {
        await Task.Delay(loadingDuration, cancellationToken);
        return ["A"];
    }
}