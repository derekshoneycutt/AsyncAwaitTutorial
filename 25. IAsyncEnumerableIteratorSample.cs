/*
 * =====================================================
 *         Step 25 : IAsyncEnumerable Iterators
 * 
 *  Now we go back and use iterator methods instead of the whole
 *  custom implementation of the interfaces. The compiler will
 *  now do all that for us, and we get much cleaner, easier to
 *  read and maintain code.
 *  
 *  A.  Copy Step 24. We will update this code.
 *  
 *  B.  The easiest way to do this is copy FirstLoop and SecondLoop
 *      from Sample 23 and convert them into async IAsyncEnumerable
 *      that include the Delay, semaphore WaitAsync, and
 *      directly yield returns the value.
 *      The custom implementation can be removed with these in place.
 *      
 *  C.  Update Run to use the iterator methods instead of the custom
 *      implementations. This should be very easy.
 *      
 *      
 *  We now have a decoupled producer/consumer pattern in our code
 *  that makes it much easier to read and maintain.
 *  We do still have the same issue that Concat is not actually running
 *  our producers at the same time, however.
 * 
 * =====================================================
*/

using System.Runtime.CompilerServices;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates construction of an IAsyncEnumerable as an async iterator method
/// </summary>
public class IAsyncEnumerableGeneratorSample : ITutorialSample
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
    /// Gets an enumerable of tasks that will produce the given range of Tasks that return the given number after a delay
    /// </summary>
    /// <param name="identifier">The identifier of the action to print the value for.</param>
    /// <param name="start">The start of the range to produce.</param>
    /// <param name="end">The end of the range to produce.</param>
    /// <param name="secondLoopSignal">The second loop signal to trigger when the loop has completed.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async IAsyncEnumerable<int> FirstLoop(
        string identifier,
        int start, int end,
        SemaphoreSlim secondLoopSignal,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // We update this to an IAsyncEnumerable that does the delay in here and yield returns the value directly

        Console.WriteLine($"Writing first producer: {identifier} / {Environment.CurrentManagedThreadId}");

        (int doStart, int doEnd) = start <= end ? (start, end) : (end, start);
        for (int value = doStart; value <= doEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            yield return value;
        }

        secondLoopSignal.Release();

        Console.WriteLine($"Fin first producer {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Gets an enumerable of tasks that will produce the given range of Tasks that return the given number after a delay
    /// </summary>
    /// <param name="identifier">The identifier of the action to print the value for.</param>
    /// <param name="start">The start of the range to produce.</param>
    /// <param name="end">The end of the range to produce.</param>
    /// <param name="secondLoopSignal">The second loop signal to wait until it is triggered to run the loop.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async IAsyncEnumerable<int> SecondLoop(
        string identifier,
        int start, int end,
        SemaphoreSlim secondLoopSignal,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // We update this to an IAsyncEnumerable that does the delay in here and yield returns the value directly

        Console.WriteLine($"Writing second producer: {identifier} / {Environment.CurrentManagedThreadId}");

        await secondLoopSignal.WaitAsync(cancellationToken).ConfigureAwait(false);

        (int doStart, int doEnd) = start <= end ? (start, end) : (end, start);
        for (int value = doStart; value <= doEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            yield return value;
        }

        Console.WriteLine($"Fin second producer {identifier} / {Environment.CurrentManagedThreadId}");
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
    /// Loops over the first of integers subsequently as an asynchronous operation
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
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        int actionCount = 55;
        List<Task> tasks = [];
        SemaphoreSlim synchronize = new(1);
        List<SemaphoreSlim> semaphores = [synchronize];
        AsyncLocal<int> mod = new();
        for (int i = 0; i < actionCount; ++i)
        {
            mod.Value = 10 * i;
            string identifier = $"Action {i}";
            SemaphoreSlim secondLoopSignal = new(0);
            semaphores.Add(secondLoopSignal);
            // We update to call the iterators now instead
            IAsyncEnumerable<int> values =
                FirstLoop(identifier,
                    1 + mod.Value, 5 + mod.Value,
                    secondLoopSignal, cancellationToken)
                    .Concat(SecondLoop(identifier,
                        1001 + mod.Value, 1005 + mod.Value,
                        secondLoopSignal, cancellationToken));
            tasks.Add(
                Consumer(identifier, values, synchronize, cancellationToken));
        }

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
