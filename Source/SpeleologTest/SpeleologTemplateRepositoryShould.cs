using Shouldly;
using SpeleoLogViewer.SpeleologTemplate;

namespace SpeleologTest;

public class SpeleologTemplateRepositoryShould
{
    [Theory]
    [InlineData("toto.speleolog", true)]
    [InlineData("toto.toto", false)]
    [InlineData("toto", false)]
    public void RecognizeExtension(string filePath, bool isTemplateFile) =>
        new SpeleologTemplateRepository().IsTemplateFile(filePath).ShouldBe(isTemplateFile);

    [Fact]
    public async Task ReadFileTemplate()
    {
        var template = await new SpeleologTemplateRepository().ReadAsync("MyTemplate.speleolog");
        template.ShouldSatisfyAllConditions(speleologTemplate =>
        {
            speleologTemplate.ShouldNotBeNull();
            speleologTemplate.Name.ShouldBe("MyTemplate");
            speleologTemplate.Files.ShouldBe(["path/log.txt"]);
        });
    }

    [Fact]
    public async Task ReturnNullWhenReadingFileWithWrongExtension() =>
        (await new SpeleologTemplateRepository().ReadAsync("MyTemplate.toto")).ShouldBeNull();

    [Fact]
    public async Task ReturnNullWhenFileNotExists() =>
        (await new SpeleologTemplateRepository().ReadAsync("MyTemplate1.speleolog")).ShouldBeNull();

    [Fact]
    public async Task WriteTemplateToDisk()
    {
        const string templateFolderPath = "TemplatesWrite";
        Directory.CreateDirectory(templateFolderPath);
        var name = Guid.NewGuid().ToString();
        List<string> files = ["log.txt"];

        await new SpeleologTemplateRepository().WriteToDiskAsync(templateFolderPath, new SpeleologTemplate(name, files));

        var filePath = Path.Combine(templateFolderPath, name + SpeleologTemplateRepository.Extension);
        (await new SpeleologTemplateRepository().ReadAsync(filePath))
            .ShouldSatisfyAllConditions(speleologTemplate =>
        {
            speleologTemplate.ShouldNotBeNull();
            speleologTemplate.Name.ShouldBe(name);
            speleologTemplate.Files.ShouldBe(files);
        });
        
        Directory.Delete(templateFolderPath,true);
    }
    
    [Fact]
    public void ReadAll()
    {
        var templateInfosList = new SpeleologTemplateRepository().ReadAll("Templates");
        templateInfosList
            .Select(infos => infos.Name)
            .ShouldBe(["MyTemplate1", "MyTemplate2", "MyTemplate3"]);

        new SpeleologTemplateRepository().ReadAsync(templateInfosList[0].Path).ShouldNotBeNull();
    }
}