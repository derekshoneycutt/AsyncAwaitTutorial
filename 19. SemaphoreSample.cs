namespace AsyncAwaitTutorial;


/// <summary>
/// Sample demonstrating the use of semaphores with asynchronous code.
/// </summary>
public class SemaphoreSample
{
    /// <summary>
    /// Loops over 2 ranges of integers subsequently as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first range start.</param>
    /// <param name="firstMax">The first range maximum.</param>
    /// <param name="secondLoopSignal">The semaphore to signal when the first loop is complete.</param>
    /// <param name="synchronize">The semaphore used to synchronize printing values to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task FirstLoop(
        string identifier,
        int firstStart, int firstMax,
        SemaphoreSlim secondLoopSignal,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Writing first values: {identifier} / {Environment.CurrentManagedThreadId}");

        for (int i = firstStart; i <= firstMax; i++)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            await synchronize.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} PRE");
                await Task.Yield();
                Console.WriteLine($"  {identifier} / {Environment.CurrentManagedThreadId} => {i}");
            }
            finally
            {
                synchronize.Release();
            }
        }

        secondLoopSignal.Release();

        Console.WriteLine($"Fin first {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Loops over 2 ranges of integers subsequently as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondMax">The second range maximum.</param>
    /// <param name="secondLoopSignal">The semaphore that signals when the first loop is complete.</param>
    /// <param name="synchronize">The semaphore used to synchronize printing values to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task SecondLoop(
        string identifier,
        int secondStart, int secondMax,
        SemaphoreSlim secondLoopSignal,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Writing second values: {identifier} / {Environment.CurrentManagedThreadId}");

        await secondLoopSignal.WaitAsync(cancellationToken).ConfigureAwait(false);

        for (int i = secondStart; i <= secondMax; i++)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            await synchronize.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} PRE");
                await Task.Yield();
                Console.WriteLine($"  {identifier} / {Environment.CurrentManagedThreadId} => {i}");
            }
            finally
            {
                synchronize.Release();
            }
        }

        Console.WriteLine($"Fin second {identifier} / {Environment.CurrentManagedThreadId}");
    }


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task Run(
        CancellationToken cancellationToken)
    {
        int threadCount = 55;
        List<Task> tasks = [];
        List<SemaphoreSlim> secondLoopSignals = [];
        SemaphoreSlim synchronize = new(1);

        for (int i = 0; i < threadCount; ++i)
        {
            int mod = 10 * i;
            string action = $"Action {i}";
            SemaphoreSlim secondLoopSignal = new(0);
            secondLoopSignals.Add(secondLoopSignal);
            tasks.Add(
                FirstLoop(action, 1 + mod, 5 + mod, secondLoopSignal, synchronize, cancellationToken));
            tasks.Add(
                SecondLoop(action, 10001 + mod, 10005 + mod, secondLoopSignal, synchronize, cancellationToken));
        }

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("Canceled");
        }
        finally
        {
            foreach (SemaphoreSlim semaphore in secondLoopSignals)
            {
                semaphore.Dispose();
            }
            synchronize.Dispose();
        }
    }
}
