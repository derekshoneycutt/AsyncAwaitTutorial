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
    public static void ThreadMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        TaskCompletionSource completionSource,
        CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

            (int start, int end) = firstStart <= firstEnd ? (firstStart, firstEnd) : (firstEnd, firstStart);
            for (int value = start; value <= end; ++value)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
            }
            (start, end) = secondStart <= secondEnd ? (secondStart, secondEnd) : (secondEnd, secondStart);
            for (int value = start; value <= end; ++value)
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
        private readonly SemaphoreSlim _semaphore = new(0);

        /// <summary>
        /// Flag indicating if this instance has been completed and no more messages should be sent.
        /// </summary>
        private volatile bool _completed = false;

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
    /// Producers the first range of values to the consumer, with a delay between each production
    /// </summary>
    /// <param name="identifier">The identifier of the producer method to report as.</param>
    /// <param name="channel">The channel to produce values onto.</param>
    /// <param name="start">The start of the range of values to produce.</param>
    /// <param name="end">The end of the range of values to produce.</param>
    /// <param name="secondLoopSignal">The semaphore that signals when the first loop is complete.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task FirstLoop(
        string identifier,
        MyChannel<int> channel,
        int start, int end,
        SemaphoreSlim secondLoopSignal,
        CancellationToken cancellationToken)
    {
        // Update to write to a channel instead of yield returning

        Console.WriteLine($"Writing first values: {identifier} / {Environment.CurrentManagedThreadId}");

        (int doStart, int doEnd) = start <= end ? (start, end) : (end, start);
        for (int value = doStart; value <= doEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            channel.Write(value);
        }

        secondLoopSignal.Release();

        Console.WriteLine($"Fin first values {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Producers the second range of values to the consumer, with a delay between each production
    /// </summary>
    /// <param name="identifier">The identifier of the producer method to report as.</param>
    /// <param name="channel">The channel to produce values onto.</param>
    /// <param name="start">The start of the range of values to produce.</param>
    /// <param name="end">The end of the range of values to produce.</param>
    /// <param name="secondLoopSignal">The semaphore that signals when the first loop is complete.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task SecondLoop(
        string identifier,
        MyChannel<int> channel,
        int start, int end,
        SemaphoreSlim secondLoopSignal,
        CancellationToken cancellationToken)
    {
        // Update to write to a channel instead of yield returning

        Console.WriteLine($"Writing second values: {identifier} / {Environment.CurrentManagedThreadId}");

        await secondLoopSignal.WaitAsync(cancellationToken).ConfigureAwait(false);

        (int doStart, int doEnd) = start <= end ? (start, end) : (end, start);
        for (int value = doStart; value <= doEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            channel.Write(value);
        }

        Console.WriteLine($"Fin second values {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Called when a new value is received in one of the main loops.
    /// </summary>
    /// <param name="identifier">The identifier of the action to print the value for.</param>
    /// <param name="value">The value to print.</param>
    /// <param name="synchronize">The semaphore used to synchronize console output.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    private static async Task OnNewValueAsync(
        string identifier,
        int value,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        await synchronize.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Console.Write($"{identifier} / {Environment.CurrentManagedThreadId}");
            await Task.Yield();
            Console.WriteLine($" / {Environment.CurrentManagedThreadId} => {value}");
        }
        finally
        {
            synchronize.Release();
        }
    }

    /// <summary>
    /// Called when a new value is received in one of the main loops.
    /// </summary>
    /// <param name="identifier">The identifier of the action to print the value for.</param>
    /// <param name="value">The value to print.</param>
    /// <param name="synchronize">The semaphore used to synchronize console output.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    private static async Task OnNewValue2Async(
        string identifier,
        int value,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        // We create a second OnNewValue in order to have a modified handling (we just throw MODIFIED in the console text)
        await synchronize.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Console.Write($"MODIFIED {identifier} / {Environment.CurrentManagedThreadId}");
            await Task.Yield();
            Console.WriteLine($" / {Environment.CurrentManagedThreadId} => {value}");
        }
        finally
        {
            synchronize.Release();
        }
    }

    /// <summary>
    /// Loops over the integers received from the channel and prints them, as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The values stream to consume.</param>
    /// <param name="synchronize">The semaphore used to synchronize printing values to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task Consumer(
        string identifier,
        IAsyncEnumerable<int> values,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Writing consumer: {identifier} / {Environment.CurrentManagedThreadId}");

        await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await OnNewValueAsync(identifier, value, synchronize, cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"Fin consumer {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Loops over the integers received from the channel and prints them with slight modification, as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The values stream to consume.</param>
    /// <param name="synchronize">The semaphore used to synchronize printing values to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task Consumer2(
        string identifier,
        IAsyncEnumerable<int> values,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        // We copy a second consumer for fun. It will also call a second OnNewValueAsync method

        Console.WriteLine($"Writing second consumer: {identifier} / {Environment.CurrentManagedThreadId}");

        await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await OnNewValue2Async(identifier, value, synchronize, cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"Fin second consumer {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        // change our action count to producers and consumers, and create the channel for comms.
        int producers = 99;
        int consumers = Environment.ProcessorCount * 2;
        MyChannel<int> channel = new();
        List<Task> tasks = [];
        SemaphoreSlim synchronize = new(1);

        //Run the consumers first this time, and add them as tasks to the final wait list
        for (int i = 0; i < consumers; ++i)
        {
            string name = $"Consumer {i}";
            if (i % 2 == 0)
            {
                _ = Consumer(name, channel.ReadAllAsync(cancellationToken), synchronize, cancellationToken);
            }
            else
            {
                _ = Consumer2(name, channel.ReadAllAsync(cancellationToken), synchronize, cancellationToken);
            }
        }

        // We'll need a special productionTasks list in order to complete the channel
        List<Task> productionTasks = [];
        List<SemaphoreSlim> semaphores = [synchronize];
        AsyncLocal<int> mod = new();
        for (int i = 0; i < producers; ++i)
        {
            mod.Value = 10 * i;
            string identifier = $"Action {i}";
            SemaphoreSlim secondLoopSignal = new(0);
            semaphores.Add(secondLoopSignal);
            // We update to call the new producer methods, adding the tasks to productionTasks
            productionTasks.Add(FirstLoop(identifier, channel,
                1 + mod.Value, 5 + mod.Value,
                secondLoopSignal, cancellationToken));
            productionTasks.Add(SecondLoop(identifier, channel,
                1001 + mod.Value, 1005 + mod.Value,
                secondLoopSignal, cancellationToken));
        }

        // Complete the channel when all production is complete and add the final task to the final wait list
        tasks.Add(Task.WhenAll(productionTasks)
            .ContinueWith(_ =>
            {
                channel.Complete();
            }, cancellationToken));

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            ThreadMethod("Single Thread",
                1, 5,
                101, 105,
                backThreadSource, cancellationToken)));
        instanceCaller.Start();
        tasks.Add(backThreadSource.Task);

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);

            Console.WriteLine("All fin");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("Canceled");
        }
        finally
        {
            foreach (SemaphoreSlim semaphore in semaphores)
            {
                semaphore.Dispose();
            }
        }
    }
}
