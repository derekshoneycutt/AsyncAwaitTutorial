/*
 * =====================================================
 *         Step 4 : Custom Thread Pool with Execution Context Sample
 * 
 *  This launches threads counted by the number of processor count
 *  (changed from just 2!) in a pool and balances multiple actions
 *  queued into the pool.
 *  The use of AsyncLocal is included in the Run method to
 *  demonstrate that thread local storage works when you run the actions
 *  on the execution context captured for them. Otherwise, it is
 *  always just 0 when run on the thread pool from the previous samples.
 *  In a lot of Windows programming, thread local storage is
 *  important concerning the UI thread. Monitoring and running actions
 *  on the execution context will cause them to run on the UI thread
 *  as is appropriate as well.
 *  Later samples will expand on this core knowledge and how to
 *  apply it in async/await.
 *  
 *  
 *  A.  Copy Step 3. We will reuse all of this.
 *      
 *  B.  (Optional) Update the threadCount in the thread pool to
 *      Environment.ProcessorCount to (maybe) give us more threads.
 *      Update the action count in Run higher to see behavior (55?)
 *      
 *  C.  Change the mod variable in Run to AsyncLocal<int>,
 *      using ThreadLocalStorage.
 *      If you run this, you will note that the behavior
 *      always acts like mod is 0 despite modifications to it.
 *      
 *  D.  Add Execution Context support to the custom thread pool
 *      by making the queue a Tuple including the context, and
 *      creating an Execute method to run actions on the execution
 *      context from the work item queue. We have to capture the
 *      context in QueueUserWorkItem to complete this.
 *      You will then note that the thread local storage works.
 *      
 * This is a difficult but extremely important topic for how
 * we can avoid problems with threading, especially when
 * we need to do work on UI thread and other issues.
 * 
 * =====================================================
*/

using System.Collections.Concurrent;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates adding ExecutionContext to the custom thread pool made in the previous sample.
/// </summary>
public class MyThreadPoolWithContextSample : ITutorialSample
{
    /// <summary>
    /// A custom thread pool class. This just maintains a static pool of 2 threads.
    /// </summary>
    public static class MyThreadPool
    {
        /// <summary>
        /// The number of threads to have in the pool
        /// We update this to the number of processors on our machine now, for (maybe) more threads
        /// </summary>
        private static readonly int _threadCount = Environment.ProcessorCount;

        /// <summary>
        /// The collection of actions to be run on the pool
        /// We change this to a tuple that also stores the execution context that is captured at the time of adding the action to the queue, if available
        /// </summary>
        private static readonly BlockingCollection<(Action, ExecutionContext?)> _actionQueue = [];

        /// <summary>
        /// Executes the specified action on the specified context, if the context is given.
        /// Now that we are going to support execution contexts, we need to always  execute the
        /// actions on the execution context associated to them.
        /// </summary>
        /// <param name="queued">The queued data to execute.</param>
        private static void Execute((Action, ExecutionContext?) queued)
        {
            (Action action, ExecutionContext? executionContext) = queued;
            if (executionContext is null)
            {
                action();
            }
            else
            {
                ExecutionContext.Run(executionContext, act => ((Action)act!).Invoke(), action);
            }
        }

        /// <summary>
        /// Static initializer for the thread pool, creates and launches the required threads
        /// </summary>
        static MyThreadPool()
        {
            for (int i = 0; i < _threadCount; ++i)
            {
                new Thread(() =>
                {
                    while (true)
                    {
                        // Run on the execution context instead of invoking directly here!
                        Execute(_actionQueue.Take());
                    }
                })
                { IsBackground = true }.Start();
            }
        }

        /// <summary>
        /// Queue an action into the work to be done in the thread pool
        /// </summary>
        /// <param name="action">The action to queue for performing in the thread pool</param>
        public static void QueueUserWorkItem(Action action)
        {
            _actionQueue.Add((action, ExecutionContext.Capture()));
        }
    }

    /// <summary>
    /// The number of actions to launch on the thread pool
    /// </summary>
    private static int _actionCount = 0;
    /// <summary>
    /// The reset event used to signal that all actions have completed processing
    /// </summary>
    private static readonly ManualResetEventSlim _resetEvent = new(false);

    /// <summary>
    /// The instance method to run as actions in the sample thread pool. This is a synchronous method.
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

        // Increase our delay to 1 full second as we can handle more!
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
        // Increased to 55 to stretch it
        int actionCount = 55;
        _actionCount = actionCount;
        // having mod on thread local storage breaks before we add ExecutionContext
        AsyncLocal<int> mod = new();
        for (int i = 0; i < _actionCount; ++i)
        {
            mod.Value = 10 * i;
            string identifier = $"Action {i}";
            MyThreadPool.QueueUserWorkItem(() =>
                InstanceMethod(identifier,
                    1 + mod.Value, 5 + mod.Value,
                    10001 + mod.Value, 10005 + mod.Value));
        }

        _resetEvent.Wait(cancellationToken);

        Console.WriteLine("All fin");
    }
}
