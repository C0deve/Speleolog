using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.LogFileViewer.V2;

public interface ITextFileLoaderV2
{
    Task<string[]> GetTextAsync(string filePath, CancellationToken cancellationToken);
}