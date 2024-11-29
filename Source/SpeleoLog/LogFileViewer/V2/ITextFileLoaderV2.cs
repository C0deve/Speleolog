namespace SpeleoLog.LogFileViewer.V2;

public interface ITextFileLoaderV2
{
    Task<string[]> GetTextAsync(string filePath, CancellationToken cancellationToken);
}