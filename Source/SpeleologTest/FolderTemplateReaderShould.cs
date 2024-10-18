using Shouldly;
using SpeleoLogViewer.SpeleologTemplate;

namespace SpeleologTest;

public class FolderTemplateReaderShould
{
    [Fact]
    public void ReadFileTemplate()
    {
        var templateInfosList = new FolderTemplateReader().Read("Templates");
        templateInfosList
            .Select(infos => infos.Name)
            .ShouldBe(["MyTemplate1", "MyTemplate2", "MyTemplate3"]);

        new SpeleologTemplateRepository().ReadAsync(templateInfosList[0].Path).ShouldNotBeNull();
    }
}