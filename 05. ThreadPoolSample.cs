/*
 * =====================================================
 *         Step 5 : Move to the standard Thread Pool
 * 
 *  Here, we just remove the custom thread pool and start
 *  using the standard ThreadPool class. This is to get us
 *  comfortable with this behavior, as it is more advanced
 *  than what we did.
 *  
 *  
 *  A.  Copy Step 4. We will reuse all of this.
 *      
 *  B.  Delete all of the custom thread pool and use ThreadPool.
 *      QueueUserWorkItem call will need updated in this.
 *      
 *  C.  Create ThreadPoolState and demonstrate starting a
 *      work item on the ThreadPool with some state.
 *      
 * This is a pretty simple one, but we can spend some time
 * reviewing everything we've learned and see how it fits directly
 * with what we have available in the standard library.
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates using the standard ThreadPool class. That's all
/// </summary>
public class ThreadPoolSample : ITutorialSample
{
    /// <summary>
    /// State structure to send to the instance method for work items queued on the thread pool
    /// </summary>
    readonly record struct ThreadPoolState(string Identifier, AsyncLocal<int> Mod);

    /// <summary>
    /// The number of actions to launch on the thread pool
    /// </summary>
    private static int _actionCount = 0;
    /// <summary>
    /// The reset event used to signal that all actions have completed processing
    /// </summary>
    private static readonly ManualResetEventSlim _resetEvent = new(false);

    /// <summary>
    /// The instance method to run as actions in the thread pool. This is a synchronous method.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    public static void InstanceMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        (int start, int end) = firstStart <= firstEnd ? (firstStart, firstEnd) : (firstEnd, firstStart);
        for (int value = start; value <= end; ++value)
        {
            Thread.Sleep(1000);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }
        (start, end) = secondStart <= secondEnd ? (secondStart, secondEnd) : (secondEnd, secondStart);
        for (int value = start; value <= end; ++value)
        {
            Thread.Sleep(1000);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");

        if (Interlocked.Decrement(ref _actionCount) < 1)
        {
            _resetEvent.Set();
        }
    }


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(CancellationToken cancellationToken)
    {
        int actionCount = 55;
        _actionCount = actionCount;
        AsyncLocal<int> mod = new();
        for (int i = 0; i < _actionCount; ++i)
        {
            mod.Value = 10 * i;
            string identifier = $"Action {i}";
            // Move to the standard ThreadPool instead; performance optimizations exist here.
            ThreadPool.QueueUserWorkItem<ThreadPoolState>(state =>
                InstanceMethod(state.Identifier,
                    1 + state.Mod.Value, 5 + state.Mod.Value,
                    1001 + state.Mod.Value, 1005 + state.Mod.Value),
                new(identifier, mod), true);
        }

        _resetEvent.Wait();

        Console.WriteLine("All fin");
    }
}

