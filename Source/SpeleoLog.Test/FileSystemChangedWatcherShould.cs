using Shouldly;
using FileSystemWatcher = SpeleoLog._Infrastructure.FileSystemWatcher;

namespace SpeleoLog.Test;

public class FileSystemChangedWatcherShould
{
    [Fact]
    public async Task EmitOnFileChangedIntegration()
    {
        var isInvoked = false;
        var filePath = Utils.CreateUniqueEmptyFile();
        var sut = new FileSystemWatcher(Path.GetDirectoryName(filePath)!);
        sut.Changed += (_, _) =>
        {
            isInvoked = true;
        };

        await File.AppendAllTextAsync(filePath, "a" + Environment.NewLine);
        await Task.Delay(TimeSpan.FromMilliseconds(10));
        
        File.Delete(filePath);
        isInvoked.ShouldBeTrue();
    }
}