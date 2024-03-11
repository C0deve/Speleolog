using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.SpeleologTemplate;

public interface ISpeleologTemplateReader
{
    Task<SpeleologTemplate?> ReadAsync(string filePath, CancellationToken token = default);
    bool IsTemplateFile(string filePath);
}