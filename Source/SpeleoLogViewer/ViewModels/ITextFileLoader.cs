using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.ViewModels;

public interface ITextFileLoader
{
    Task<string> GetTextAsync(string filePath, CancellationToken cancellationToken);
}