using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.ViewModels;

public interface ITextFileLoader
{
    Task<IEnumerable<string>> GetTextAsync(string filePath, CancellationToken cancellationToken);
}