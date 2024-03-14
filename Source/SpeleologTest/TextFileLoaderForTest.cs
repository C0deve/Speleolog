using SpeleoLogViewer.LogFileViewer;

namespace SpeleologTest;

public class TextFileLoaderForTest(Func<string, string> getText) : ITextFileLoader
{
    public TextFileLoaderForTest(string[] text) : this(_ =>  string.Join(Environment.NewLine, text)) { }
    
    public Task<string> GetTextAsync(string filePath, CancellationToken cancellationToken) => 
        Task.FromResult(getText(filePath));
}