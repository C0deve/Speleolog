using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.SpeleologTemplate;

public class SpeleologTemplateReader : ISpeleologTemplateReader
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

        return speleologTemplate with { Name = filePath.Replace(Extension, "") };
    }

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}