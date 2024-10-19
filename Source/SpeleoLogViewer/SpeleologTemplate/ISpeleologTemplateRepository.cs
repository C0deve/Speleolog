using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.SpeleologTemplate;

public interface ISpeleologTemplateRepository
{
    Task<SpeleologTemplate?> ReadAsync(string filePath, CancellationToken token = default);
    bool IsTemplateFile(string filePath);
    Task WriteToDiskAsync(string folderPath, SpeleologTemplate speleologTemplate, CancellationToken token = default);
    IReadOnlyList<TemplateInfos> ReadAll(string folderPath);
}