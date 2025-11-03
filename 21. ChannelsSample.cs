using System.Threading.Channels;

namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates utilizing Channels to bring all prior lessons together coherently
/// </summary>
public static class ChannelsSample
{
    /// <summary>
    /// Produces values from 2 ranges subsequently into the given channel as an asynchronous operation.
    /// </summary>
    /// <param name="channelWriter">The channel to write values into.</param>
    /// <param name="firstStart">The first range start.</param>
    /// <param name="firstEnd">The first range end.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondEnd">The second range end.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
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


    /// <summary>
    /// Consumes a channel, printing out any values that come across, as they come across, as an asynchronous operation.
    /// </summary>
    /// <param name="channelReader">The channel reader to consume.</param>
    /// <param name="identifier">The identifier to note when printing results.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
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



    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task Run(
        CancellationToken cancellationToken)
    {
        Channel<int> channel = Channel.CreateUnbounded<int>();

        int producers = 5;
        int consumers = 9;

        List<Task> producerTasks = [];
        List<Task> consumerTasks = [];

        for (int i = 0; i < producers; ++i)
        {
            int mod = i == 0 ? 0 : unchecked((int)Math.Pow(10.0, i));
            producerTasks.Add(Producer(
                channel.Writer, 1 + mod, 5 + mod, 1000000001 + mod, 1000000005 + mod, cancellationToken));
        }

        Task producerHost = Task.WhenAll(producerTasks)
            .ContinueWith(_ =>
            {
                channel.Writer.Complete();
            }, cancellationToken);

        for (int i = 0; i < consumers; ++i)
        {
            string name = $"Consumer {i}";
            consumerTasks.Add(Consumer(channel.Reader, name, cancellationToken));
        }


        await Task.WhenAll([producerHost, .. consumerTasks]).ConfigureAwait(false);
    }
}
