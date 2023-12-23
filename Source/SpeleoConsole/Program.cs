// See https://aka.ms/new-console-template for more information

Console.WriteLine("Hello, World!");

const string file = @"D:\Veranda\test.txt";
var directoryName = Path.GetDirectoryName(file) ?? throw new ApplicationException($"Impossible de trouver le repertoir du fichier {file}");
var fileName = Path.GetFileName(file);
FileSystemWatcher fsw = new(directoryName, fileName);
fsw.EnableRaisingEvents = true;
fsw.NotifyFilter = NotifyFilters.LastWrite;
fsw.IncludeSubdirectories = false;
fsw.Changed += (_, eventArgs) => {Console.WriteLine($"{_.GetType()}{eventArgs.Name} {eventArgs.ChangeType}");};
Console.ReadLine();