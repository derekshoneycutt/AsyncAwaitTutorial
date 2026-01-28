using System.Threading.Channels;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates adding a middleman to an existing structured channels pipeline.
/// </summary>
public class ChannelMiddlemanSample : ITutorialSample
{
    /// <summary>
    /// The instance method to run as independent threads in the sample. This is a synchronous method.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    /// <param name="completionSource">The Task Completion Source to mark when this task has completed</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static void DoubleLoop(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        TaskCompletionSource completionSource,
        CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

            for (int value = firstStart; value <= firstEnd; ++value)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
            }
            for (int value = secondStart; value <= secondEnd; ++value)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");

            completionSource.SetResult();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            completionSource.SetCanceled(cancellationToken);
        }
        catch (Exception ex)
        {
            completionSource.SetException(ex);
        }
    }

    /// <summary>
    /// Producer class used to generate integer values and send them over a channel
    /// </summary>
    public class Producer(int count)
    {
        /// <summary>
        /// The channel used to communicate the values
        /// </summary>
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

        /// <summary>
        /// Reads all values as an asynchronous collection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A <see cref="IAsyncEnumerable{Int32}"/> that iterates each time a new value is produced.</returns>
        public IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken) =>
            _channel.Reader.ReadAllAsync(cancellationToken);

        /// <summary>
        /// Produces the specified ranges of values.
        /// </summary>
        /// <param name="firstStart">The first start value.</param>
        /// <param name="firstEnd">The first maximum value, completing the first range.</param>
        /// <param name="secondStart">The second start value.</param>
        /// <param name="secondEnd">The second maximum value, completing the second range.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A Task that completes when the asynchronous operation has finished.</returns>
        private async Task Produce(
            int firstStart, int firstEnd, int secondStart, int secondEnd,
            CancellationToken cancellationToken)
        {
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
        }

        /// <summary>
        /// Runs the specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        public async Task Run(CancellationToken cancellationToken)
        {
            // For the run, we have basically the same code to launch the producers as before, but now isolated and OOP-ish
            List<Task> productionTasks = [];
            for (int index = 0; index < count; ++index)
            {
                int mod = 10 * index;
                productionTasks.Add(Produce(
                    1 + mod, 5 + mod,
                    1001 + mod, 1005 + mod,
                    cancellationToken));
            }
            await Task.WhenAll(productionTasks).ConfigureAwait(false);
            _channel.Writer.Complete();
        }
    }

    /// <summary>
    /// Middleman that intercepts messages between the primary producer and consumers
    /// </summary>
    public class Middleman
    {
        // We add a new Middleman class that is a little bit consumer, a little bit producer

        /// <summary>
        /// The channel used to communicate the values
        /// </summary>
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

        /// <summary>
        /// Reads all values as an asynchronous collection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A <see cref="IAsyncEnumerable{Int32}"/> that iterates each time a new value is produced.</returns>
        public IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken) =>
            _channel.Reader.ReadAllAsync(cancellationToken);

        /// <summary>
        /// The last value
        /// </summary>
        private int? _lastValue = null;

        /// <summary>
        /// The synchronize semaphore used to ensure compiling values are thread safe.
        /// </summary>
        private readonly SemaphoreSlim _synchronize = new(1);

        /// <summary>
        /// Consumes the specified values.
        /// </summary>
        /// <param name="values">The values to consume.</param>
        /// <param name="cancellationToken">The cancellation token used to signal when the operation should not proceed.</param>
        /// <returns>A Task that completes when the asynchronous operation has finished.</returns>
        private async Task Consume(
            IAsyncEnumerable<int> values,
            CancellationToken cancellationToken)
        {
            await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await _synchronize.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (_lastValue is null)
                    {
                        _lastValue = value;
                    }
                    else
                    {
                        await _channel.Writer.WriteAsync(
                            (100000 * _lastValue.Value) + value,
                            cancellationToken).ConfigureAwait(false);
                        _lastValue = null;
                    }
                }
                finally
                {
                    _synchronize.Release();
                }
            }
        }


        /// <summary>
        /// Intercepts a communication pipeline.
        /// </summary>
        /// <param name="values">The values to consume for the middleman.</param>
        /// <param name="cancellationToken">The cancellation token used to signal when the operation should not proceed.</param>
        public async Task Intercept(
            IAsyncEnumerable<int> values,
            CancellationToken cancellationToken)
        {
            List<Task> consumers = [];
            for (int index = 1; index <= 666; ++index)
            {
                consumers.Add(Consume(
                    values,
                    cancellationToken));
            }
            await Task.WhenAll(consumers).ConfigureAwait(false);
            _channel.Writer.Complete();

        }
    }

    /// <summary>
    /// Consumer class used to print values to the screen
    /// </summary>
    public class Consumer
    {

        /// <summary>
        /// Consumes the collection, printing each value to the screen
        /// </summary>
        /// <param name="identifier">The identifier to print as the name of the current instance.</param>
        /// <param name="values">The values to print to the screen.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A Task that completes when the asynchronous operation has finished.</returns>
        private async Task Consume(
            string identifier,
            IAsyncEnumerable<int> values,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

            await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
            }

            Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
        }

        /// <summary>
        /// Runs the consumers.
        /// </summary>
        /// <param name="values">The values to consume.</param>
        /// <param name="cancellationToken">The cancellation token used to signal when the operation should not proceed.</param>
        public async Task Run(
            IAsyncEnumerable<int> values,
            CancellationToken cancellationToken)
        {
            List<Task> consumers = [];
            for (int index = 1; index <= Environment.ProcessorCount * 2; ++index)
            {
                string identifier = $"Action {index}";
                consumers.Add(Consume(identifier, values, cancellationToken));
            }
            await Task.WhenAll(consumers).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        Producer producer = new(55);
        Consumer consumer = new();
        // We add the middleman and inject it between the producer and consumer
        Middleman middleman = new();
        _ = consumer.Run(middleman.ReadAllAsync(cancellationToken), cancellationToken);
        _ = middleman.Intercept(producer.ReadAllAsync(cancellationToken), cancellationToken);
        
        List<Task> tasks = [producer.Run(cancellationToken)];

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            DoubleLoop("Single Thread",
                1, 5,
                101, 105,
                backThreadSource, cancellationToken)));
        instanceCaller.Start();
        tasks.Add(backThreadSource.Task);

        await Task.WhenAll(tasks).ConfigureAwait(false);

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);

        Console.WriteLine("All fin");
    }
}
