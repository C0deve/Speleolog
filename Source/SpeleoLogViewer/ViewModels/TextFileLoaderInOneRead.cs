using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.ViewModels;

public class TextFileLoaderInOneRead : ITextFileLoader
{
    public async Task<string> GetTextAsync(string filePath, CancellationToken cancellationToken) => 
        await File.ReadAllTextAsync(filePath, cancellationToken);
}