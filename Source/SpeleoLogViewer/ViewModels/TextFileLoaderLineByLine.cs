using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.ViewModels;

public class TextFileLoaderLineByLine : ITextFileLoader
{
    public async Task<IEnumerable<string>> GetTextAsync(string filePath, CancellationToken cancellationToken) => 
        await File.ReadAllLinesAsync(filePath, cancellationToken);
}