﻿// See https://aka.ms/new-console-template for more information

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



// Write the string array to a new file named "WriteLines.txt".
using var outputFile = new StreamWriter(Path.Combine(@"D:\Veranda", "sample.txt"), true);
for (var i = 0; i < 10000; i++)
    outputFile.WriteLine("Bla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla bla");