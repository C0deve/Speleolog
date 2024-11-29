namespace SpeleoLog.Viewer.Core;

public interface IFileLoader
{
    Task<string[]> GetTextAsync(string filePath, CancellationToken cancellationToken);
}