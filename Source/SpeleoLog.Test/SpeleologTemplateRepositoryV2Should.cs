using Shouldly;
using SpeleoLog.SpeleologTemplate;

namespace SpeleoLog.Test;

public class SpeleologTemplateRepositoryV2Should
{
    [Theory]
    [InlineData("toto.v2.slg", true)]
    [InlineData("toto.toto", false)]
    [InlineData("toto", false)]
    public void RecognizeExtension(string filePath, bool isTemplateFile) =>
        new SpeleologTemplateRepositoryV2().IsTemplateFile(filePath).ShouldBe(isTemplateFile);

    [Fact]
    public async Task ReadFromTemplateInfos()
    {
        (await new SpeleologTemplateRepositoryV2().ReadAsync(
                new TemplateInfos(
                    "MyTemplate.v2.slg",
                    "MyTemplate")))
            .ShouldSatisfyAllConditions(speleologTemplate =>
            {
                speleologTemplate.ShouldNotBeNull();
                speleologTemplate.Name.ShouldBe("MyTemplate");
                speleologTemplate.Files.ShouldBe(["path/log.txt", "path2/log2.txt"]);
            });
    }

    [Fact]
    public async Task ReturnNullWhenReadingFileWithWrongExtension() =>
        (await new SpeleologTemplateRepositoryV2().ReadAsync("MyTemplate.toto")).ShouldBeNull();

    [Fact]
    public async Task ReturnNullWhenFileNotExists() =>
        (await new SpeleologTemplateRepositoryV2().ReadAsync("MyTemplate1.v2.slg")).ShouldBeNull();

    [Fact]
    public async Task SaveTemplateToDisk()
    {
        const string templateFolderPath = "TemplatesWrite";
        Directory.CreateDirectory(templateFolderPath);
        var name = Guid.NewGuid().ToString();
        string[] files = ["log.txt"];

        var sut = new SpeleologTemplateRepositoryV2();
        var template = new SpeleologTemplate.SpeleologTemplate(name, files);
        await sut.SaveAsync(templateFolderPath, template);

        (await sut.ReadAsync(sut.ReadAll(templateFolderPath)[0]))
            .ShouldSatisfyAllConditions(speleologTemplate =>
            {
                speleologTemplate.ShouldNotBeNull();
                speleologTemplate.Name.ShouldBe(name);
                speleologTemplate.Files.ShouldBe(files);
            });

        Directory.Delete(templateFolderPath, true);
    }

    [Fact]
    public void ReadAll()
    {
        const string folderPath = "TemplatesV2";
        var templateInfosList = new SpeleologTemplateRepositoryV2().ReadAll(folderPath);
        templateInfosList
            .Select(infos => infos.Name)
            .ShouldBe(["MyTemplate1", "MyTemplate2", "MyTemplate3"]);

        new SpeleologTemplateRepositoryV2().ReadAsync(templateInfosList[0]).ShouldNotBeNull();
    }
}