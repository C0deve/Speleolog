// See https://aka.ms/new-console-template for more information

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

var path = Path.Combine(@"D:\Veranda", "sample4.txt");

Console.WriteLine("Hello, World!");

const string text1 = """
                     Le Lorem Ipsum est simplement du faux texte employé dans la composition et la mise en page avant impression. Le Lorem Ipsum est le faux texte standard de l'imprimerie depuis les années 1500, quand un imprimeur anonyme assembla ensemble des morceaux de texte pour réaliser un livre spécimen de polices de texte.
                     Il n'a pas fait que survivre cinq siècles, mais s'est aussi adapté à la bureautique informatique, sans que son contenu n'en soit modifié. Il a été popularisé dans les années 1960 grâce à la vente de feuilles Letraset contenant des passages du Lorem Ipsum, et, plus récemment, par son inclusion dans des applications de mise en page de texte, comme Aldus PageMaker.
                     """;

const string text = "Le Lorem Ipsum est simplement du faux texte employé dans la composition et la mise en page avant impression. Le Lorem Ipsum est le faux texte standard de l'imprimerie depuis les années 1500, quand un imprimeur anonyme assembla ensemble des morceaux de texte pour réaliser un livre spécimen de polices de texte.";

// const string file = @"D:\Veranda\test.txt";
// var directoryName = Path.GetDirectoryName(file) ?? throw new ApplicationException($"Impossible de trouver le repertoir du fichier {file}");
// var fileName = Path.GetFileName(file);
// FileSystemWatcher fsw = new(directoryName, fileName);
// fsw.EnableRaisingEvents = true;
// fsw.NotifyFilter = NotifyFilters.LastWrite; 
// fsw.IncludeSubdirectories = false;
// fsw.Changed += (o, eventArgs) => {Console.WriteLine($"{o.GetType()}{eventArgs.Name} {eventArgs.ChangeType}");};
// Console.ReadLine();

var interrupteur = new BehaviorSubject<bool>(false);
var speed = new BehaviorSubject<int>(5);
interrupteur.AsObservable()
    .Select(b => b
        ? speed.AsObservable().Select(theSpeed =>
                Observable
                    .Interval(TimeSpan.FromSeconds(theSpeed))
                    .Select(_ => Observable.FromAsync(async () =>
                    {
                        var lines = await File.ReadAllLinesAsync(path);
                        await File.AppendAllLinesAsync(path, GenerateRows(lines.Length));
                    }))
                    .Concat())
            .Switch()
        : Observable.Return(Unit.Default))
    .Switch()
    .Subscribe();

var k = "";

while (k != "x")
{
    Console.WriteLine("Please enter a number (x to exit / on to start / off to stop):");
    k = Console.ReadLine();
    if (int.TryParse(k, out var aSpeed))
    {
        speed.OnNext(aSpeed);
        continue;
    }

    switch (k)
    {
        case "on":
            interrupteur.OnNext(true);
            break;
        case "off":
            interrupteur.OnNext(false);
            break;
    }
}

Console.WriteLine("Bye bye");
return;

IEnumerable<string> GenerateRows(int result) =>
    Enumerable.Range(result + 1, 10)
        .Select(i => $"{i} {text}");
//k = Console.ReadLine();
// Write the string array to a new file named "WriteLines.txt".
// using var outputFile = new StreamWriter(path, true);
// Enumerable.Range(0,1543).Select(i => $"{i}").ToList().ForEach(outputFile.WriteLine);
// for (var i = 0; i < 10000; i++)
//     outputFile.WriteLine("Bla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla blaBla bla bla");