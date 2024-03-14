using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.LogFileViewer;

public interface ITextFileLoader
{
    Task<string> GetTextAsync(string filePath, CancellationToken cancellationToken);
}