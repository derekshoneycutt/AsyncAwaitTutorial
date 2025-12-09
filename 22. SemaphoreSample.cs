/*
 * =====================================================
 *         Step 22 : Semaphores
 * 
 *  Coordinating concurrent operations is an often difficult task
 *  for many developers, and coordinating asynchronous operations
 *  often presents with similar concerns. Unfortunately, we also
 *  run into limitations of async code that many Wait operations
 *  on standard concurrency structures block the thread they are
 *  called on. This makes them contraindicated in async code,
 *  because we want to avoid blocking any threads on the thread pool.
 *  Nonetheless, Semaphores work great with async, allowing us
 *  to continue writing locking code without concerns.
 *  The point of this sample is to demonstrate a couple of potential
 *  uses of semaphore in coordinating async operations.
 *  
 *  A.  Copy Step 20. We will update this code.
 *  
 *  B.  First, create a OnNewValueAsync method that will be
 *      called each time we want to print a value to the screen.
 *      We will take a string identifier, int value,
 *      SemaphoreSlim synchronize, and cancellationToken.
 *      In this, we want to take our ordinary print value
 *      Console.WriteLine line and split it into 2 code lines.
 *      The first will call Console.Write and be followed by
 *      a Task.Yield that gives up the current thread for
 *      use on the thread pool. The second line will finish
 *      the started text with Console.WriteLine.
 *      Wrap these in a WaitAsync and Release on the passed
 *      synchronize SemaphoreSlim.
 *      
 *  C.  Update the existing InstanceMethod to call the new
 *      OnNewValueAsync. We will need to make a SemaphoreSlim
 *      in the Run method, with an initialCount of 1, and pass
 *      it in here to then pass down to the OnNewValueAsync.
 *      Because we are not wrapping literally every
 *      Console.Write/Line call in this semaphore, we may be
 *      able to note some race conditions occurring
 *      sometimes when we execute, specifically with the additional
 *      background thread. However, this semaphore protects
 *      the two lines we wrapped so that at least this one printing
 *      is never running over each other in a race condition.
 *       
 *  D.  In Run, create a list of SemaphoreSlim objects, the
 *      first being the synchronize Semaphore added.
 *      In each iteration that we call our instance method,
 *      create a new SemaphoreSlim, secondLoopSignal, with initialCount 0,
 *      and we add it to the list of Semaphores. At the end of Run,
 *      we need to Dispose this list of semaphores.
 *      
 *  E.  Now, we want to split InstanceMethod into 2 methods.
 *      One will be the first loop and will Release secondLoopSignal
 *      when it is complete. The other will be the second loop
 *      and will WaitAsync on secondLoopSignal until the first
 *      loop releases. We need to then replace our original
 *      single call with 2 calls to these 2 methods.
 *      
 *      
 * At the end of this, we have successfully decoupled our two
 * loops into two separate methods, and we ensured that we have
 * some imperfect protections against race conditions when we're
 * printing values to the screen in each loop.
 * This is quite a bit of work, but we can see multiple asynchronous
 * methods interacting with each other now and coordinating
 * with async compatible locking and signaling.
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// Sample demonstrating the use of semaphores with asynchronous code.
/// </summary>
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

    // We copy InstanceMethod into 2 and modify them some one does only the first loop and the other does only the second loop

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

        (int start, int end) = firstStart <= firstEnd ? (firstStart, firstEnd) : (firstEnd, firstStart);
        for (int value = start; value <= end; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            // Inside, we wait with the synchronization semaphore and then release it so no other task tries to write at the same time as us
            await OnNewValueAsync(identifier, value, synchronize, cancellationToken).ConfigureAwait(false);
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

        (int start, int end) = secondStart <= secondEnd ? (secondStart, secondEnd) : (secondEnd, secondStart);
        for (int value = start; value <= end; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            // Same modification inside as the first loop
            await OnNewValueAsync(identifier, value, synchronize, cancellationToken).ConfigureAwait(false);
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
            string identifier = $"Action {i}";
            // Create a semaphore that must be Released at least once; this is the signal to run the second loop
            SemaphoreSlim secondLoopSignal = new(0);
            semaphores.Add(secondLoopSignal);
            // We call both first loop and second loop together;
            // second loop will sit on the queue waiting for the second loop signal to trigger it to start
            tasks.Add(
                FirstLoop(identifier,
                    1 + mod.Value, 5 + mod.Value,
                    secondLoopSignal, synchronize, cancellationToken));
            tasks.Add(
                SecondLoop(identifier,
                    1001 + mod.Value, 1005 + mod.Value,
                    secondLoopSignal, synchronize, cancellationToken));
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
