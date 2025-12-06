using System.Threading.Channels;

namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates utilizing Channels in a structured way to demonstrate a stream of values from a central producer class.
/// </summary>
public static class StructuredChannelsSample
{
    /// <summary>
    /// Producer class used to generate integer values and 
    /// </summary>
    public class Producer(int count)
    {
        /// <summary>
        /// The channel used to communicate the values
        /// </summary>
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

        /// <summary>
        /// Produces values from 2 ranges subsequently into the given channel as an asynchronous operation.
        /// </summary>
        /// <param name="firstStart">The first range start.</param>
        /// <param name="firstEnd">The first range end.</param>
        /// <param name="secondStart">The second range start.</param>
        /// <param name="secondEnd">The second range end.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public async Task Produce(
            int firstStart, int firstEnd, int secondStart, int secondEnd,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"Producing on {Environment.CurrentManagedThreadId}...");

            for (int value = firstStart; value <= firstEnd; ++value)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                await _channel.Writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
            }
            for (int value = secondStart; value <= secondEnd; ++value)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                await _channel.Writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
            }

            Console.WriteLine($"Fin Prod on {Environment.CurrentManagedThreadId}");
        }

        /// <summary>
        /// Runs the specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        public async Task Run(CancellationToken cancellationToken)
        {
            List<Task> producerTasks = [];
            for (int i = 0; i < count; ++i)
            {
                int mod = i == 0 ? 0 : unchecked((int)Math.Pow(10.0, i));
                producerTasks.Add(Produce(
                    1 + mod, 5 + mod, 1000000001 + mod, 1000000005 + mod, cancellationToken));
            }

            await Task.WhenAll(producerTasks);

            _channel.Writer.Complete();
        }

        /// <summary>
        /// Reads all values as an asynchronous collection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A <see cref="IAsyncEnumerable{Int32}"/> that iterates each time a new value is produced.</returns>
        public IAsyncEnumerable<int> ReadAllValuesAsync(CancellationToken cancellationToken) =>
            _channel.Reader.ReadAllAsync(cancellationToken);
    }

    /// <summary>
    /// Called when a new value is received, as an asynchronous operation.
    /// </summary>
    /// <param name="value">The value that was received.</param>
    /// <param name="identifier">The identifier of the consumer that called this instance.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    private static async Task OnNewValue(
        int value,
        string identifier,
        CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"Consumer cancelled during new value {identifier}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in new value for consumer {identifier}: {ex.Message}");
        }
    }

    /// <summary>
    /// Consumes a channel, printing out any values that come across, as they come across, as an asynchronous operation.
    /// </summary>
    /// <param name="producer">The producer of values to consume.</param>
    /// <param name="identifier">The identifier to note when printing results.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task Consumer(
        Producer producer,
        string identifier,
        CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"Consuming starting on {identifier} / {Environment.CurrentManagedThreadId}");

            await foreach (int value in producer.ReadAllValuesAsync(cancellationToken).ConfigureAwait(false))
            {
                await OnNewValue(value, identifier, cancellationToken).ConfigureAwait(false);
            }

            Console.WriteLine($"Fin Cons {identifier} / {Environment.CurrentManagedThreadId}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"Consumer cancelled {identifier}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in consumer {identifier}: {ex.Message}");
        }
    }



    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task Run(
        CancellationToken cancellationToken)
    {
        int producers = 5;
        int consumers = 9;

        List<Task> consumerTasks = [];

        Producer producer = new(producers);
        Task producerHost = producer.Run(cancellationToken);

        for (int i = 0; i < consumers; ++i)
        {
            string name = $"Consumer {i}";
            consumerTasks.Add(Consumer(producer, name, cancellationToken));
        }

        await Task.WhenAll([producerHost, .. consumerTasks]).ConfigureAwait(false);
    }
}
