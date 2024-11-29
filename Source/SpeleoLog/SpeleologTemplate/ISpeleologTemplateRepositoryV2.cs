namespace SpeleoLog.SpeleologTemplate;

public interface ISpeleologTemplateRepositoryV2
{
    bool IsTemplateFile(string filePath);
    Task SaveAsync(string folderPath, SpeleologTemplate speleologTemplate, CancellationToken token = default);
    IReadOnlyList<TemplateInfos> ReadAll(string folderPath);
    Task<SpeleologTemplate?> ReadAsync(string filePath, CancellationToken token = default);
    Task<SpeleologTemplate?> ReadAsync(TemplateInfos templateInfos, CancellationToken token = default);
}