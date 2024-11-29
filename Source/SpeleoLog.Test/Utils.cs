using System.Reflection;

namespace SpeleoLog.Test;

public static class Utils
{
    public static string CreateUniqueEmptyFile()
    {
        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException();
        var fileName = $"{Guid.NewGuid()}.txt";
        var folder = Path.Combine(path, "TestFiles");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, fileName);
        using var f = File.CreateText(filePath);
        f.Close();
        return filePath;
    }
}