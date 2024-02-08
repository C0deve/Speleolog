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
    public async Task ReadFileV2()
    {
        var param = await File.ReadAllTextAsync("Speleologv2.json");
        JsonSerializer
            .Deserialize<SpeleologState>(param, Options)
            .ShouldNotBeNull()
            .AppendFromBottom
            .ShouldBeTrue();
    }

    [Fact]
    public async Task WriteReadFile()
    {
        List<string> lastOpenFiles = ["log.txt", "log2.txt"];
        var expectedState = new SpeleologState(lastOpenFiles, false);

        await new SpeleologStateRepository().SaveAsync(expectedState);

        (await new SpeleologStateRepository().GetAsync())
            .ShouldNotBeNull()
            .LastOpenFiles
            .ShouldBe(lastOpenFiles);
    }
}