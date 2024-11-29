using SpeleoLog.Viewer.Core;

namespace SpeleoLog.Test;

public class FileLoaderForTest(Func<string, string> getText) : IFileLoader
{
    public FileLoaderForTest(params string[] text) : this(_ =>  string.Join(Environment.NewLine, text)) { }
    
    public Task<string[]> GetTextAsync(string filePath, CancellationToken cancellationToken) => 
        Task.FromResult(new[] {getText(filePath)});
}