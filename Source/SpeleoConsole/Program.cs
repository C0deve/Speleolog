// See https://aka.ms/new-console-template for more information

var path = Path.Combine(@"D:\Veranda", "sample4.txt");

Console.WriteLine("Hello, World!");

// const string file = @"D:\Veranda\test.txt";
// var directoryName = Path.GetDirectoryName(file) ?? throw new ApplicationException($"Impossible de trouver le repertoir du fichier {file}");
// var fileName = Path.GetFileName(file);
// FileSystemWatcher fsw = new(directoryName, fileName);
// fsw.EnableRaisingEvents = true;
// fsw.NotifyFilter = NotifyFilters.LastWrite; 
// fsw.IncludeSubdirectories = false;
// fsw.Changed += (o, eventArgs) => {Console.WriteLine($"{o.GetType()}{eventArgs.Name} {eventArgs.ChangeType}");};
// Console.ReadLine();


var k = "";

while (k != "x")
{
    Console.WriteLine("Please enter a number (x to exit):");
    k = Console.ReadLine();
    if (int.TryParse(k, out var range))
    {
        var lines = await File.ReadAllLinesAsync(path);
        if (int.TryParse(lines.LastOrDefault() ?? "0", out var lastInt))
            await File.AppendAllLinesAsync(path, Enumerable.Range(lastInt + 1, range).Select(i => $"{i}"));
    }
}

Console.WriteLine("Bye bye");
//k = Console.ReadLine();
// Write the string array to a new file named "WriteLines.txt".
// using var outputFile = new StreamWriter(path, true);
// Enumerable.Range(0,1543).Select(i => $"{i}").ToList().ForEach(outputFile.WriteLine);
// for (var i = 0; i < 10000; i++)
//     outputFile.WriteLine("Bla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla bla");