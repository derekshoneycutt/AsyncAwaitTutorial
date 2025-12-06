using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates a custom, basic channel class that shows the basic motivations and how it works
/// </summary>
public static class MyChannelSample
{
    /// <summary>
    /// Custom channels class used to send messages from producers to consumers
    /// </summary>
    /// <typeparam name="T">The message type to communicate</typeparam>
    public class MyChannel<T>
    {
        /// <summary>
        /// The queue of messages to read
        /// </summary>
        private readonly ConcurrentQueue<T> _queue = new();

        /// <summary>
        /// The semaphore that is used to signal consumers that a new item is available.
        /// </summary>
        private readonly SemaphoreSlim _semaphore = new(0);

        /// <summary>
        /// Flag indicating if this instance has been completed and no mroe messages should be sent.
        /// </summary>
        private bool _completed = false;

        /// <summary>
        /// Writes the specified value from the consumer.
        /// </summary>
        public void Write(T value)
        {
            _queue.Enqueue(value);
            _semaphore.Release();
        }

        /// <summary>
        /// Completes this instance.
        /// </summary>
        public void Complete()
        {
            _completed = true;
            _semaphore.Release();
        }

        /// <summary>
        /// Reads all messages that are available as an asynchronous collection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>An asynchronous collection that iterates each time a new message is available.</returns>
        public async IAsyncEnumerable<T> ReadAllAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (!_completed)
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (!_completed && _queue.TryDequeue(out T? next) && (next is not null))
                {
                    yield return next;
                }
                else if (_completed)
                {
                    _semaphore.Release();
                }
            }
        }
    }


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
        MyChannel<int> channelWriter,
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Producing on {Environment.CurrentManagedThreadId}...");

        for (int value = firstStart; value <= firstEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            channelWriter.Write(value);
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            channelWriter.Write(value);
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
        MyChannel<int> channelReader,
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
        MyChannel<int> channel = new();

        int producers = 5;
        int consumers = 9;

        List<Task> producerTasks = [];
        List<Task> consumerTasks = [];

        for (int i = 0; i < producers; ++i)
        {
            int mod = i == 0 ? 0 : unchecked((int)Math.Pow(10.0, i));
            producerTasks.Add(Producer(
                channel, 1 + mod, 5 + mod, 1000000001 + mod, 1000000005 + mod, cancellationToken));
        }

        Task producerHost = Task.WhenAll(producerTasks)
            .ContinueWith(_ =>
            {
                channel.Complete();
            }, cancellationToken);

        for (int i = 0; i < consumers; ++i)
        {
            string name = $"Consumer {i}";
            consumerTasks.Add(Consumer(channel, name, cancellationToken));
        }


        await Task.WhenAll([producerHost, .. consumerTasks]).ConfigureAwait(false);
    }
}
