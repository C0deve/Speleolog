﻿using System.Text.Json;

namespace SpeleoLog.ApplicationState;

public class StateRepository
{
    private const string FilePath = "SpeleologStateV1.json";

    private readonly JsonSerializerOptions? _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SaveAsync(SpeleologState state, CancellationToken token = default)
    {
        if (File.Exists(FilePath)) File.Delete(FilePath); 
        var json = JsonSerializer.Serialize(state, _options);
        await File.WriteAllTextAsync(FilePath, json, token).ConfigureAwait(false);
    }

    public async Task<SpeleologState?> GetAsync(CancellationToken token = default)
    {
        if (!File.Exists(FilePath)) return null;
        var param = await File.ReadAllTextAsync(FilePath, token).ConfigureAwait(false);
        return JsonSerializer.Deserialize<SpeleologState>(param, _options);
    }

    public SpeleologState? Get()
    {
        if (!File.Exists(FilePath)) return null;
        var param = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<SpeleologState>(param, _options);
    }
}