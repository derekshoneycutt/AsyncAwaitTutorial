using System.Threading;

namespace AsyncAwaitTutorial;


/// <summary>
/// Sample demonstrating the use of semaphores with asynchronous code.
/// </summary>
/// <remarks>
/// We take the code from Sample 20 -- Cancellation Tokens Sample -- and refactor the asynchronous method into 2 separate asynchronous methods.
/// In order to coordinate them, we use a SemaphoreSlim to signal when the second loop should start iterating, and another SemaphoreSlim
/// to ensure that only one of the asynchronous instances is writing at a single time.
/// </remarks>
public class SemaphoreSample : ITutorialSample
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


    // We copy InstanceMethod into 2 and modify them some one does only the first loop and the other does only the second loop

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
        // We use Console.Write and then Task.Yield so that we may even switch to a different thread before
        // finishing writing. This would be ripe for race conditions without a semaphore coordinating only 1 instance accessing
        // writing a value to the console at a time.
        // Sometimes, we might see this with the Fin lines either way (take a few runs and watch closely!)
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
    /// Loops over the first of integers subsequently as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first range start.</param>
    /// <param name="firstEnd">The first range maximum.</param>
    /// <param name="secondLoopSignal">The semaphore to signal when the first loop is complete.</param>
    /// <param name="synchronize">The semaphore used to synchronize printing values to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task FirstLoop(
        string identifier,
        int firstStart, int firstEnd,
        SemaphoreSlim secondLoopSignal,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Writing first values: {identifier} / {Environment.CurrentManagedThreadId}");

        for (int i = firstStart; i <= firstEnd; i++)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            // Inside, we wait with the synchronization semaphore and then release it so no other task tries to write at the same time as us
            await OnNewValue(identifier, i, synchronize, cancellationToken).ConfigureAwait(false);
        }

        // When the first loop is done, we signal to run the second with the second loop signal semaphore.
        secondLoopSignal.Release();

        Console.WriteLine($"Fin first {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Loops over the second range of integers subsequently as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondEnd">The second range maximum.</param>
    /// <param name="secondLoopSignal">The semaphore that signals when the first loop is complete.</param>
    /// <param name="synchronize">The semaphore used to synchronize printing values to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task SecondLoop(
        string identifier,
        int secondStart, int secondEnd,
        SemaphoreSlim secondLoopSignal,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Writing second values: {identifier} / {Environment.CurrentManagedThreadId}");

        // For the second loop, we just hang out and wait until the second loop signal semaphore is released
        await secondLoopSignal.WaitAsync(cancellationToken).ConfigureAwait(false);

        for (int i = secondStart; i <= secondEnd; i++)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            // Same modification inside as the first loop
            await OnNewValue(identifier, i, synchronize, cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"Fin second {identifier} / {Environment.CurrentManagedThreadId}");
    }


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        // We will just use the cancellation token passed in from now on, without linking our own.

        int actionCount = 55;
        List<Task> tasks = [];
        // We need a single synchronization semaphore that allows 1 resource access at a time,
        // and we need to track all the semaphores that we're using altogether, including the synchronization one
        SemaphoreSlim synchronize = new(1);
        List<SemaphoreSlim> semaphores = [synchronize];
        AsyncLocal<int> mod = new();
        for (int i = 0; i < actionCount; ++i)
        {
            mod.Value = 10 * i;
            string action = $"Action {i}";
            // Create a semaphore that must be Released at least once; this is the signal to run the second loop
            SemaphoreSlim secondLoopSignal = new(0);
            semaphores.Add(secondLoopSignal);
            // We call both first loop and second loop together;
            // second loop will sit on the queue waiting for the second loop signal to trigger it to start
            tasks.Add(
                FirstLoop(action, 1 + mod.Value, 5 + mod.Value, secondLoopSignal, synchronize, cancellationToken));
            tasks.Add(
                SecondLoop(action, 10001 + mod.Value, 10005 + mod.Value, secondLoopSignal, synchronize, cancellationToken));
        }

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            ThreadMethod("Single Thread", 1, 5, 101, 105, backThreadSource, cancellationToken)));
        instanceCaller.Start();
        tasks.Add(backThreadSource.Task);

        try
        {
            // We don't want to cancel here any more

            await Task.WhenAll(tasks).ConfigureAwait(false);

            Console.WriteLine("All fin");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("Canceled");
        }
        finally
        {
            // When all is done, we need to dispose our semaphores
            foreach (SemaphoreSlim semaphore in semaphores)
            {
                semaphore.Dispose();
            }
        }
    }
}
