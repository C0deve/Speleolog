using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.SpeleologTemplate;

public class SpeleologTemplateRepositoryV2 : ISpeleologTemplateRepositoryV2
{
    private const string Extension = ".slg";
    private const string Version = "v2";

    public bool IsTemplateFile(string filePath) =>
        Path.GetExtension(filePath).Equals(Extension, StringComparison.InvariantCultureIgnoreCase);

    public async Task<SpeleologTemplate?> ReadAsync(TemplateInfos templateInfos, CancellationToken token = default)
    {
        if (!File.Exists(templateInfos.Path)) return null;
        
        var files = await ReadFilesFromContent(templateInfos.Path, token);
        
        return new SpeleologTemplate(templateInfos.Name, files);
    }
    
    public async Task<SpeleologTemplate?> ReadAsync(string filePath, CancellationToken token = default)
    {
        if (!IsTemplateFile(filePath)) return null;
        //throw new ApplicationException($"Template file should have {Extension} extension. Actual {Path.GetExtension(filePath)}");

        if (!File.Exists(filePath)) return null;
        
        var name = GetTemplateNameFromFilePath(filePath);
        var files = await ReadFilesFromContent(filePath, token);
        
        return new SpeleologTemplate(name, files);
    }
    
    public async Task SaveAsync(string folderPath, SpeleologTemplate speleologTemplate, CancellationToken token = default)
    {
        if(!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        var filePath = Path.Combine(folderPath, $"{speleologTemplate.Name}.v2{Extension}");
        await File.WriteAllLinesAsync(filePath, speleologTemplate.Files, token);
    }

    public IReadOnlyList<TemplateInfos> ReadAll(string folderPath)
    {
        try
        {
            return Directory
                .EnumerateFiles(folderPath, $"*{Extension}")
                .Select(filePath => new TemplateInfos(filePath, GetTemplateNameFromFilePath(filePath)))
                .ToImmutableArray();
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
    }
    private static string GetTemplateNameFromFilePath(string filePath) => 
        Path.GetFileNameWithoutExtension(filePath).Replace($".{Version}","");
    private static async Task<string[]> ReadFilesFromContent(string filePath, CancellationToken token) => 
        await File.ReadAllLinesAsync(filePath, token).ConfigureAwait(false);
}
