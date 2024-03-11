using Shouldly;
using SpeleoLogViewer.SpeleologTemplate;

namespace SpeleologTest;

public class SpeleologTemplateReaderShould
{
    [Theory]
    [InlineData("toto.speleolog", true)]
    [InlineData("toto.toto", false)]
    [InlineData("toto", false)]
    public void RecognizeExtension(string filePath, bool isTemplateFile) =>
        new SpeleologTemplateReader().IsTemplateFile(filePath).ShouldBe(isTemplateFile);

    [Fact]
    public async Task ReadFileTemplate()
    {
        var template = await new SpeleologTemplateReader().ReadAsync("MyTemplate.speleolog");
        template
            .ShouldNotBeNull()
            .Name.ShouldBe("MyTemplate");
        template.Files.ShouldBe(["path/log.txt"]);
    }

    [Fact]
    public async Task ReturnNullWhenReadingFileWithWrongExtension() =>
        (await new SpeleologTemplateReader().ReadAsync("MyTemplate.toto")).ShouldBeNull();

    [Fact]
    public async Task ReturnNullWhenFileNotExists() =>
        (await new SpeleologTemplateReader().ReadAsync("MyTemplate1.speleolog")).ShouldBeNull();
}