using SpeleoLogViewer.LogFileViewer.V2;

namespace SpeleologTest;

public class TextFileLoaderV2ForTest(Func<string, string> getText) : ITextFileLoaderV2
{
    public TextFileLoaderV2ForTest(params string[] text) : this(_ =>  string.Join(Environment.NewLine, text)) { }
    
    public Task<string[]> GetTextAsync(string filePath, CancellationToken cancellationToken) => 
        Task.FromResult(new[] {getText(filePath)});
}