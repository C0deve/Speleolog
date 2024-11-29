namespace SpeleoLog._Infrastructure;

public class FileLoader : IFileLoader
{
    private int _index;

    /// <summary>
    /// Return all 'unread' lines from filePath
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>Keeps of the number of lines read</remarks>
    public Task<string[]> GetTextAsync(string filePath, CancellationToken cancellationToken) => Task.Run(() =>
    {
        using var inStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(inStream);

        // skipping the old data
        for (var i = 0; i < _index; i++)
            sr.ReadLine();

        var result = new List<string>();
        while (sr.Peek() >= 0) // reading the live data if exists
        {
            var str = sr.ReadLine();
            if (str == null) continue;
            result.Add(str);
            _index++;
        }
        
        sr.Close();
        return result.ToArray();
        
    }, cancellationToken);
}