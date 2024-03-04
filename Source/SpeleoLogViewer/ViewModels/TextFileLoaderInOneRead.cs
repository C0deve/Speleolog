using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.ViewModels;

public class TextFileLoaderInOneRead : ITextFileLoader
{
    public async Task<IEnumerable<string>> GetTextAsync(string filePath, CancellationToken cancellationToken)
    {
        var allText = await File.ReadAllTextAsync(filePath, cancellationToken);
        return allText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }
}