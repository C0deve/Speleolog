using Shouldly;
using SpeleoLogViewer.LogFileViewer;

namespace SpeleologTest;

public class SequencerShould
{
    private readonly List<string> _messages = new();
    private readonly TimeSpan _duration = TimeSpan.FromMilliseconds(200);

    [Fact]
    public async Task DoOneThingOnly()
    {
        var sut = Sequencer();

        sut.Enqueue(Do);
        sut.Enqueue(Do);

        await Task.Delay(_duration * 1.1);
        _messages.ShouldBe(["Hello"]);
    }

    [Fact]
    public async Task DoAll()
    {
        var sut = Sequencer();

        sut.Enqueue(Do);
        sut.Enqueue(Do);

        await Task.Delay(3 * _duration);
        _messages.ShouldBe(["Hello", "Hello"]);
    }

    [Fact]
    public async Task WaitAll()
    {
        var sut = Sequencer();

        sut.Enqueue(Do);
        sut.Enqueue(Do);

        await sut.WaitAll();
        _messages.ShouldBe(["Hello", "Hello"]);
    }

    [Fact]
    public async Task HandleException()
    {
        var sut =
            Sequencer()
                .Enqueue(DoWithException)
                .Enqueue(Do);

        await sut.WaitAll();
        _messages.ShouldBe(["Something went wrong", "Hello"]);
    }

    private Sequencer<string> Sequencer()
    {
        var sut = new Sequencer<string>(exception => _messages.Add(exception.Message));
        sut.Output.Subscribe(
            message => _messages.Add(message),
            exception => _messages.Add(exception.Message)
        );

        return sut;
    }

    private string Do()
    {
        Thread.Sleep(_duration);
        return "Hello";
    }

    private string DoWithException()
    {
        Thread.Sleep(_duration / 2);
        throw new Exception("Something went wrong");
    }
}