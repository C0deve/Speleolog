using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SpeleoLogViewer.LogFileViewer.V2;

namespace SpeleoLogViewer.LogFileViewer;

public class TextFileLoaderV2 : ITextFileLoaderV2
{
    private int _index;

    public Task<string[]> GetTextAsync(string filePath, CancellationToken cancellationToken) => Task.Run(() =>
    {
        using var inStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(inStream);

        // skipping the old data, it has read in the Form1_Load event handler
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