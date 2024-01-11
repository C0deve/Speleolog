using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.Models;

public static class SpeleologStateRepository
{
    private const string FilePath = "SpeleologStateV1.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task SaveAsync(SpeleologState state, CancellationToken token = default)
    {
        if (File.Exists(FilePath)) File.Delete(FilePath); 
        var json = JsonSerializer.Serialize(state, Options);
        await File.WriteAllTextAsync(FilePath, json, token).ConfigureAwait(false);
    }

    public static async Task<SpeleologState?> GetAsync(CancellationToken token = default)
    {
        if (!File.Exists(FilePath)) return null;
        var param = await File.ReadAllTextAsync(FilePath, token).ConfigureAwait(false);
        return JsonSerializer.Deserialize<SpeleologState>(param, Options);
    }
}