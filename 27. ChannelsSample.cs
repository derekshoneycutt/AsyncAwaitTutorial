/*
 * =====================================================
 *         Step 27 : Standard Channels
 * 
 *  Now that we have a good concept of Channels from making a cheap
 *  version of our own, we switch over to the much more featureful
 *  standard Channels structures. Overall, our code remains the same,
 *  with just a few tweaks.
 *  
 *  A.  Copy Step 26. We will update this code.
 *  
 *  B.  Remove the custom Channel implementation and replace all
 *      references with the standard channel implementation.
 *      We will just use an Unbounded channel for now,
 *      and our Producers will receive ChannelWriter instead of
 *      the whole thing--maintaining our separation of concerns.
 *      
 *      
 *  We can now make robust asynchronous code and utilize
 *  Channels to decouple our producers and consumers, allowing
 *  for multiple of either, and a great deal of control over both.
 * 
 * =====================================================
*/

using System.Threading.Channels;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates utilizing the standard Channels in Producer/Consumer asynchronous pattern.
/// </summary>
public class ChannelsSample : ITutorialSample
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

    // No more custom Channels class; note how much we use ChannelReader and ChannelWriter instead now!

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
        ChannelWriter<int> channel,
        int start, int end,
        SemaphoreSlim secondLoopSignal,
        CancellationToken cancellationToken)
    {
        // We take in a ChannelWriter<int> now, instead of just Channel

        Console.WriteLine($"Writing first values: {identifier} / {Environment.CurrentManagedThreadId}");

        (int doStart, int doEnd) = start <= end ? (start, end) : (end, start);
        for (int value = doStart; value <= doEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            // the standard channels have async write
            await channel.WriteAsync(value, cancellationToken).ConfigureAwait(false);
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
        ChannelWriter<int> channel,
        int start, int end,
        SemaphoreSlim secondLoopSignal,
        CancellationToken cancellationToken)
    {
        // We take in a ChannelWriter<int> now, instead of just Channel

        Console.WriteLine($"Writing second values: {identifier} / {Environment.CurrentManagedThreadId}");

        await secondLoopSignal.WaitAsync(cancellationToken).ConfigureAwait(false);

        (int doStart, int doEnd) = start <= end ? (start, end) : (end, start);
        for (int value = doStart; value <= doEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            // the standard channels have async write
            await channel.WriteAsync(value, cancellationToken).ConfigureAwait(false);
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
        int producers = 99;
        int consumers = Environment.ProcessorCount * 2;
        // We create an Unbounded channel here, but we could use Bounded or PrioritizedBounded, or use the options, etc. We don't need to for now.
        Channel<int> channel = Channel.CreateUnbounded<int>();
        List<Task> tasks = [];
        SemaphoreSlim synchronize = new(1);

        for (int i = 0; i < consumers; ++i)
        {
            string name = $"Consumer {i}";
            // Must call ReadAllAsync on the Reader now
            if (i % 2 == 0)
            {
                _ = Consumer(name, channel.Reader.ReadAllAsync(cancellationToken), synchronize, cancellationToken);
            }
            else
            {
                _ = Consumer2(name, channel.Reader.ReadAllAsync(cancellationToken), synchronize, cancellationToken);
            }
        }

        List<Task> productionTasks = [];
        List<SemaphoreSlim> semaphores = [synchronize];
        AsyncLocal<int> mod = new();
        for (int i = 0; i < producers; ++i)
        {
            mod.Value = 10 * i;
            string identifier = $"Action {i}";
            SemaphoreSlim secondLoopSignal = new(0);
            semaphores.Add(secondLoopSignal);
            productionTasks.Add(FirstLoop(identifier, channel,
                1 + mod.Value, 5 + mod.Value,
                secondLoopSignal, cancellationToken));
            productionTasks.Add(SecondLoop(identifier, channel,
                1001 + mod.Value, 1005 + mod.Value,
                secondLoopSignal, cancellationToken));
        }

        tasks.Add(Task.WhenAll(productionTasks)
            .ContinueWith(_ =>
            {
                // Complete on the writer now
                channel.Writer.Complete();
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
