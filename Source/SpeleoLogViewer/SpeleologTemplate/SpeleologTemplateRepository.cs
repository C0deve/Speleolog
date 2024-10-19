using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.SpeleologTemplate;

public class SpeleologTemplateRepository : ISpeleologTemplateRepository
{
    public const string Extension = ".speleolog";

    public bool IsTemplateFile(string filePath) =>
        Path.GetExtension(filePath).Equals(Extension, StringComparison.InvariantCultureIgnoreCase);

    public async Task<SpeleologTemplate?> ReadAsync(string filePath, CancellationToken token = default)
    {
        if (!IsTemplateFile(filePath)) return null;
        //throw new ApplicationException($"Template file should have {Extension} extension. Actual {Path.GetExtension(filePath)}");

        if (!File.Exists(filePath)) return null;

        var fileContent = await File.ReadAllTextAsync(filePath, token).ConfigureAwait(false);
        var speleologTemplate = JsonSerializer.Deserialize<SpeleologTemplate>(fileContent, _options);
        if (speleologTemplate is null) return null;

        return speleologTemplate with { Name = Path.GetFileName(filePath).Replace(Extension, "") };
    }

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task WriteToDiskAsync(string folderPath, SpeleologTemplate speleologTemplate, CancellationToken token = default)
    {
        var fileName = Path.Combine(folderPath, $"{speleologTemplate.Name}{Extension}");
        await using var createStream = File.Create(fileName);
        await JsonSerializer.SerializeAsync(createStream, speleologTemplate, _options, cancellationToken: token);
    }
    
    public IReadOnlyList<TemplateInfos> ReadAll(string folderPath)
    {
        try
        {
            return Directory
                .EnumerateFiles(folderPath, $"*{SpeleologTemplateRepository.Extension}")
                .Select(filePath => new TemplateInfos(filePath))
                .ToImmutableArray();
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
    }
}