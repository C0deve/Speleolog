using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.LogFileViewer;

public class TextFileLoaderLineByLine : ITextFileLoader
{
    public async Task<string> GetTextAsync(string filePath, CancellationToken cancellationToken) => 
       string.Join(Environment.NewLine, await File.ReadAllLinesAsync(filePath, cancellationToken));
}