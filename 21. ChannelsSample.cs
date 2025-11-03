using System.CommandLine;
using System.Threading.Channels;

namespace AsyncAwaitTutorial;


public static class ChannelsSample
{
    public static async Task Producer(
        ChannelWriter<int> channelWriter,
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Producing on {Environment.CurrentManagedThreadId}...");

        for (int value = firstStart; value <= firstEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            await channelWriter.WriteAsync(value, cancellationToken).ConfigureAwait(false);
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            await channelWriter.WriteAsync(value, cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"Fin Prod on {Environment.CurrentManagedThreadId}");
    }


    public static async Task Consumer(
        ChannelReader<int> channelReader,
        string identifier,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Consuming starting on {Environment.CurrentManagedThreadId}");

        await foreach (int value in channelReader.ReadAllAsync(
            cancellationToken).ConfigureAwait(false))
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin Cons {Environment.CurrentManagedThreadId}");
    }


    public static async Task Run(
        ParseResult parseResult, CancellationToken cancellationToken)
    {
        Channel<int> channel = Channel.CreateUnbounded<int>();

        List<Task> producerTasks = [];
        List<Task> consumerTasks = [];

        producerTasks.Add(Producer(channel.Writer, 1, 5, 11, 15, cancellationToken));
        producerTasks.Add(Producer(channel.Writer, 101, 105, 1001, 1005, cancellationToken));
        producerTasks.Add(Producer(channel.Writer, 10001, 10005, 100001, 100005, cancellationToken));
        producerTasks.Add(Producer(channel.Writer, 100001, 100005, 1000001, 1000005, cancellationToken));
        producerTasks.Add(Producer(channel.Writer, 1000001, 1000005, 10000001, 10000005, cancellationToken));

        Task producerHost = Task.WhenAll(producerTasks)
            .ContinueWith(_ =>
            {
                channel.Writer.Complete();
            }, cancellationToken);

        consumerTasks.Add(Consumer(channel.Reader, "Reader 1", cancellationToken));
        consumerTasks.Add(Consumer(channel.Reader, "Reader 2", cancellationToken));
        consumerTasks.Add(Consumer(channel.Reader, "Reader 3", cancellationToken));
        consumerTasks.Add(Consumer(channel.Reader, "Reader 4", cancellationToken));
        consumerTasks.Add(Consumer(channel.Reader, "Reader 5", cancellationToken));
        consumerTasks.Add(Consumer(channel.Reader, "Reader 6", cancellationToken));
        consumerTasks.Add(Consumer(channel.Reader, "Reader 7", cancellationToken));
        consumerTasks.Add(Consumer(channel.Reader, "Reader 8", cancellationToken));
        consumerTasks.Add(Consumer(channel.Reader, "Reader 9", cancellationToken));


        await Task.WhenAll([producerHost, .. consumerTasks]).ConfigureAwait(false);
    }
}
