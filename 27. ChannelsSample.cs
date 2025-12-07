using System.Threading.Channels;

namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates utilizing the standard Channels in Producer/Consumer asynchronous pattern.
/// </summary>
/// <remarks>
/// We really just take the previous sample and drop the custom channels for standard channels here.
/// </remarks>
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

            for (int i = firstStart; i <= firstEnd; i++)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
            }
            for (int i = secondStart; i <= secondEnd; i++)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
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
    public static async Task FirstProducer(
        string identifier,
        ChannelWriter<int> channel,
        int start, int end,
        SemaphoreSlim secondLoopSignal,
        CancellationToken cancellationToken)
    {
        // We take in a ChannelWriter<int> now, instead of just Channel
        Console.WriteLine($"Producing first values: {identifier} / {Environment.CurrentManagedThreadId}");

        for (int i = start; i <= end; ++i)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            // the standard channels have async write
            await channel.WriteAsync(i, cancellationToken).ConfigureAwait(false);
        }

        secondLoopSignal.Release();

        Console.WriteLine($"Fin first production {identifier} / {Environment.CurrentManagedThreadId}");
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
    public static async Task SecondProducer(
        string identifier,
        ChannelWriter<int> channel,
        int start, int end,
        SemaphoreSlim secondLoopSignal,
        CancellationToken cancellationToken)
    {
        // We take in a ChannelWriter<int> now, instead of just Channel
        Console.WriteLine($"Producing second values: {identifier} / {Environment.CurrentManagedThreadId}");

        await secondLoopSignal.WaitAsync(cancellationToken).ConfigureAwait(false);

        for (int i = start; i <= end; ++i)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            // the standard channels have async write
            await channel.WriteAsync(i, cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"Fin second production {identifier} / {Environment.CurrentManagedThreadId}");
    }


    /// <summary>
    /// Called when a new value is received in one of the main loops.
    /// </summary>
    /// <param name="identifier">The identifier of the action to print the value for.</param>
    /// <param name="value">The value to print.</param>
    /// <param name="synchronize">The semaphore used to synchronize console output.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    private static async Task OnNewValue(
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
    private static async Task OnNewValue2(
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
    /// <param name="channel"></param>
    /// <param name="synchronize">The semaphore used to synchronize printing values to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task Consumer(
        string identifier,
        ChannelReader<int> channel,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        // We take in a ChannelReader<int> now, instead of just Channel
        Console.WriteLine($"Consuming values: {identifier} / {Environment.CurrentManagedThreadId}");

        await foreach (int value in channel.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            await OnNewValue2(identifier, value, synchronize, cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"Fin consuming {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Loops over the integers received from the channel and prints them with slight modification, as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="channel"></param>
    /// <param name="synchronize">The semaphore used to synchronize printing values to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task Consumer2(
        string identifier,
        ChannelReader<int> channel,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        // We take in a ChannelReader<int> now, instead of just Channel
        Console.WriteLine($"Second consuming values: {identifier} / {Environment.CurrentManagedThreadId}");

        await foreach (int value in channel.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            await OnNewValue(identifier, value, synchronize, cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"Fin second consuming {identifier} / {Environment.CurrentManagedThreadId}");
    }


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        int producers = 5;
        int consumers = 9;
        // We create an Unbounded channel here, but we could use Bounded or PrioritizedBounded, or use the options, etc. We don't need to for now.
        Channel<int> channel = Channel.CreateUnbounded<int>();
        List<Task> productionTasks = [];
        List<Task> tasks = [];
        SemaphoreSlim synchronize = new(1);
        List<SemaphoreSlim> semaphores = [synchronize];
        AsyncLocal<int> mod = new();
        for (int i = 0; i < producers; ++i)
        {
            mod.Value = 10 * i;
            string action = $"Action {i}";
            SemaphoreSlim secondLoopSignal = new(0);
            semaphores.Add(secondLoopSignal);
            productionTasks.Add(
                FirstProducer(action, channel, 1 + mod.Value, 5 + mod.Value, secondLoopSignal, cancellationToken));
            productionTasks.Add(
                SecondProducer(action, channel, 10001 + mod.Value, 10005 + mod.Value, secondLoopSignal, cancellationToken));
        }

        tasks.Add(Task.WhenAll(productionTasks)
            .ContinueWith(_ =>
            {
                // Have to complete the writer here
                channel.Writer.Complete();
            }, cancellationToken));

        for (int i = 0; i < consumers; ++i)
        {
            string name = $"Consumer {i}";
            if (i % 2 == 0)
            {
                _ = Consumer(name, channel, synchronize, cancellationToken);
            }
            else
            {
                _ = Consumer2(name, channel, synchronize, cancellationToken);
            }
        }

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            ThreadMethod("Single Thread", 1, 5, 101, 105, backThreadSource, cancellationToken)));
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
