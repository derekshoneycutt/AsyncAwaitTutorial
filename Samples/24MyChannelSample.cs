/*
 * =====================================================
 *         Step 26 : Custom Channels
 * 
 *  The last few samples have a significant problem in that
 *  the Concat operation does not actually make our producers
 *  run in parallel. However, that was a part of our original
 *  design that we continue to write code against. We will now
 *  introduce Channels by creating a custom Channel class and
 *  utilizing it to demonstrate multiple producers and consumers
 *  operating in parallel on the single channel of data.
 *  
 *  A.  Copy Step 25. We will update this code.
 *  
 *  B.  Create a custom Channels class.
 *      This will need a ConcurrentQueue to store the messages,
 *      a SemaphoreSlim to notify consumers when a new item is
 *      available on the queue, and a volatile bool to flag whether
 *      the channel is completed yet.
 *      We will add Write, ReadAllAsync, and Complete methods.
 *      
 *  C.  Update FirstLoop and SecondLoop to take in an instance
 *      of our custom channel type. Instead of yield returning a value,
 *      make them write the values to the channel. This will
 *      turn them into normal async Task methods again.
 *      
 *  D.  (Optional) Add a second Consumer method that prints to
 *      the screen somehow differently. This will give us a great
 *      view of how multiple consumers actually behave on
 *      channels.
 *      
 *  E.  Update Run. We will need to create an instance of the Channel,
 *      and pass this into FirstLoop and SecondLoop. We can pass
 *      the IAsyncEnumerable from ReadAllAsync directly into the
 *      existing Consumers, but it might be good to launch all of
 *      our consumers before the producers.
 *      In this sample, I create 99 producers and twice the number
 *      of available CPU cores of consumers, making the consumers
 *      a bottleneck.
 *      We also will have to close the channel once all producers
 *      are finished producing.
 *      
 *      
 *  Finally, we have achieved a decoupling of our code that
 *  enables to do a lot of interesting work, including multiple
 *  producers and consumers without having to be significantly
 *  aware of how the other is operating in any way.
 * 
 * =====================================================
*/

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates a custom, basic channel class that shows the basic motivations and how it works
/// </summary>
public class MyChannelSample : ITutorialSample
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
    /// Custom channels class used to send messages from producers to consumers
    /// </summary>
    /// <typeparam name="T">The message type to communicate</typeparam>
    public class MyChannel<T>
    {
        /// <summary>
        /// The queue of messages to read
        /// </summary>
        private readonly ConcurrentQueue<T> _queue = [];

        /// <summary>
        /// The semaphore that is used to signal consumers that a new item is available.
        /// </summary>
        private readonly SemaphoreSlim _signal = new(0);

        /// <summary>
        /// Flag indicating if this instance has been completed and no more messages should be sent.
        /// </summary>
        private volatile bool _completed = false;

        /// <summary>
        /// Writes the specified value from the consumer.
        /// </summary>
        public void Write(T value)
        {
            lock (_signal)
            {
                if (_completed)
                {
                    throw new InvalidOperationException();
                }

                _queue.Enqueue(value);
                _signal.Release();
            }
        }

        /// <summary>
        /// Completes this instance.
        /// </summary>
        public void Complete()
        {
            lock (_signal)
            {
                if (_completed)
                {
                    throw new InvalidOperationException();
                }

                _completed = true;
                _signal.Release();
            }
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
                await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (!_completed && _queue.TryDequeue(out T? next) && (next is not null))
                {
                    yield return next;
                }
                else if (_completed)
                {
                    _signal.Release();
                }
            }
        }
    }

    /// <summary>
    /// Produces the specified ranges of values.
    /// </summary>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task Produce(
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        MyChannel<int> channel,
        CancellationToken cancellationToken)
    {
        for (int value = firstStart; value <= firstEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            channel.Write(value);
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            channel.Write(value);
        }
    }

    /// <summary>
    /// Consumes the collection, printing each value to the screen
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The values to print to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task Consume(
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
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        // change our action count to producers and consumers, and create the channel for comms.
        MyChannel<int> channel = new();

        for (int index = 1; index <= Environment.ProcessorCount * 2; ++index)
        {
            string identifier = $"Action {index}";
            _ = Consume(identifier, channel.ReadAllAsync(cancellationToken), cancellationToken);
        }

        List<Task> tasks = [];
        for (int index = 1; index <= 55; ++index)
        {
            int mod = 10 * index;
            string identifier = $"Action {index}";
            tasks.Add(Produce(
                1 + mod, 5 + mod,
                1001 + mod, 1005 + mod,
                channel,
                cancellationToken));
        }

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
        channel.Complete();

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);

        Console.WriteLine("All fin");
    }
}
