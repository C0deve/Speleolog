using System.Text.Json;
using Shouldly;
using SpeleoLogViewer.ApplicationState;

namespace SpeleologTest;

public class SpeleologStateRepositoryShould
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
    public async Task ReadFileV3()
    {
        var param = await File.ReadAllTextAsync("Speleologv3.json");
        JsonSerializer
            .Deserialize<SpeleologState>(param, Options)
            .ShouldNotBeNull()
            .TemplateFolder
            .ShouldBe("pathToTheFolder");
    }

    [Fact]
    public async Task WriteReadFile()
    {
        List<string> lastOpenFiles = ["log.txt", "log2.txt"];
        var expectedState = new SpeleologState(lastOpenFiles, false);

        await new StateRepository().SaveAsync(expectedState);

        (await new StateRepository().GetAsync())
            .ShouldNotBeNull()
            .LastOpenFiles
            .ShouldBe(lastOpenFiles);
    }
}