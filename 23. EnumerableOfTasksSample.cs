/*
 * =====================================================
 *         Step 23 : Enumerable of Tasks?
 * 
 *  Now that we have reached a point where we can coordinate
 *  multiple asynchronous Tasks running multiple things together,
 *  we want to think back about the Producer/Consumer pattern
 *  that our synchronous IEnumerable code was able to give us.
 *  Right now, although we have decoupled our two lists,
 *  the production of values and the printing of those values
 *  to the screen is still extremely coupled.
 *  We will take a first pass with only the tools that we
 *  already have to try to create some kind of reasonable decoupling
 *  before adding new tools.
 *  
 *  A.  Copy Step 22. We will update this code.
 *  
 *  B.  First, we want to write 2 DelayOnNumberAsync methods.
 *      The first will take just a number and cancellation token,
 *      and return the number after await Task.Delay.
 *      The second will also take a SemaphoreSlim waitOnSignal
 *      and WaitAsync on it before the delay.
 *      
 *  C.  Next, we modify FirstLoop and SecondLoop to return
 *      IEnumerable<Task<int>> and instead of Console.Write,
 *      we want to yield return a call to DelayOnNumberAsync
 *      with our produced value. SecondLoop will need to
 *      call the version of DelayOnNumberAsync with the
 *      secondLoopSignal to wait on the signal before printing,
 *      and before the loop, and the loop will then need to
 *      begin advanced one already.
 *      
 *  D.  We need to add a new Consumer method that takes an
 *      IEnumerable<Task<int>> and iterates through it,
 *      awaiting on the tasks and printing the values to
 *      the screen.
 *      
 *  E.  Update Run to compile the returns of FirstLoop
 *      and SecondLoop into a single IEnumerable via Concat.
 *      Also add a call to Consumer to consume this concat'ed
 *      enumerable.
 *      
 *      
 *  With this, we have a more asynchronous producer/consumer
 *  separation. However, it is quite ugly, and if you review
 *  the output closely, we can see that our 2 producer loops
 *  are no longer technically running in parallel at all.
 *  We will want to advance this to something far more robust,
 *  but it gives us a feel for the direction we will be heading.
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates an enumerable of tasks to introduce the logic of IAsyncEnumerable
/// </summary>
public class EnumerableOfTasksSample : ITutorialSample
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

    // We create new methods, but because we are limited, we have to make a separate
    // async and iterator methods. The iterators will call the async method and yield return the Task<int>
    // as a means of "asynchronous production"

    /// <summary>
    /// Delays for a second and then returns a given number as an asynchronous operation.
    /// </summary>
    /// <param name="number">The number to return.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A <see cref="Task{Int32}"/> that represents the asynchronous operation. <c>Result</c> contains the specified integer.</returns>
    public static async Task<int> DelayOnNumberAsync(
        int number,
        CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        return number;
    }

    /// <summary>
    /// Delays for a second and then returns a given number as an asynchronous operation.
    /// </summary>
    /// <param name="number">The number to return.</param>
    /// <param name="waitOnSignal">The signal to wait on before the delay and before sending the number.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A <see cref="Task{Int32}"/> that represents the asynchronous operation. <c>Result</c> contains the specified integer.</returns>
    public static async Task<int> DelayOnNumberAsync(
        int number, SemaphoreSlim waitOnSignal,
        CancellationToken cancellationToken)
    {
        await waitOnSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        return number;
    }

    /// <summary>
    /// Gets an enumerable of tasks that will produce the given range of Tasks that return the given number after a delay
    /// </summary>
    /// <param name="identifier">The identifier of the action to print the value for.</param>
    /// <param name="start">The start of the range to produce.</param>
    /// <param name="end">The end of the range to produce.</param>
    /// <param name="secondLoopSignal">The second loop signal to trigger when the loop has completed.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static IEnumerable<Task<int>> FirstLoop(
        string identifier,
        int start, int end,
        SemaphoreSlim secondLoopSignal,
        CancellationToken cancellationToken)
    {
        // We update this to yield return a Task<int> for the number, instead of printing directly to screen

        Console.WriteLine($"Writing first values: {identifier} / {Environment.CurrentManagedThreadId}");

        (int doStart, int doEnd) = start <= end ? (start, end) : (end, start);
        for (int value = doStart; value <= doEnd; ++value)
        {
            yield return DelayOnNumberAsync(value, cancellationToken);
        }

        secondLoopSignal.Release();

        Console.WriteLine($"Fin first values {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Gets an enumerable of tasks that will produce the given range of Tasks that return the given number after a delay
    /// </summary>
    /// <param name="identifier">The identifier of the action to print the value for.</param>
    /// <param name="start">The start of the range to produce.</param>
    /// <param name="end">The end of the range to produce.</param>
    /// <param name="secondLoopSignal">The second loop signal to wait until it is triggered to run the loop.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static IEnumerable<Task<int>> SecondLoop(
        string identifier,
        int start, int end,
        SemaphoreSlim secondLoopSignal,
        CancellationToken cancellationToken)
    {
        // We update this to yield return a Task<int> for the number, instead of printing directly to screen

        Console.WriteLine($"Writing second values: {identifier} / {Environment.CurrentManagedThreadId}");

        yield return DelayOnNumberAsync(start, secondLoopSignal, cancellationToken);

        (int doStart, int doEnd) = start <= end ? (start, end) : (end, start);
        for (int value = doStart + 1; value <= doEnd; ++value)
        {
            yield return DelayOnNumberAsync(value, cancellationToken);
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
        // In the sample here, we moved this down below First and Second loop because of the new structure
        // this is just a style choice

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
        IEnumerable<Task<int>> values,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        // We create a new Consumer method, extracting all of the text printing from before

        Console.WriteLine($"Writing consumer: {identifier} / {Environment.CurrentManagedThreadId}");

        foreach (Task<int> valueTask in values)
        {
            int value = await valueTask.ConfigureAwait(false);

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
            // We update to Concat the SecondLoop onto the FirstLoop in an IEnumerable
            // and call the new Consumer
            IEnumerable<Task<int>> values =
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
