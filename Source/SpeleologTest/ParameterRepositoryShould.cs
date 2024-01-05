using System.Text.Json;
using Shouldly;
using SpeleoLogViewer.Models;

namespace SpeleologTest;

public class ParameterRepositoryShould
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task ReadFile()
    {
        var param = await File.ReadAllTextAsync("Speleolog.json");
        JsonSerializer
            .Deserialize<SpeleologState>(param, Options)
            .ShouldNotBeNull()
            .LastOpenFiles
            .Count
            .ShouldBe(1);
    }

    [Fact]
    public async Task WriteReadFile()
    {
        List<string> lastOpenFiles = ["log.txt", "log2.txt"];
        var expectedState = new SpeleologState(lastOpenFiles);

        await SpeleologStateRepository.SaveAsync(expectedState);

        (await SpeleologStateRepository.GetAsync())
            .ShouldNotBeNull()
            .LastOpenFiles
            .ShouldBe(lastOpenFiles);
    }
}