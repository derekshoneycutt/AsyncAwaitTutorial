/*
 * =====================================================
 *         Step 3 : Custom Thread Pool Sample
 * 
 *  This launches 2 threads in a pool and balances
 *  multiple actions queued into the pool. This replaces
 *  the previous list of Threads for the thread pool,
 *  requiring some additional overhead.
 *  The point is not to re-create the standard ThreadPool
 *  exactly, per se, but to demonstrate the basic patterns
 *  and concepts that drive the standard ThreadPool.
 *  
 *  
 *  A.  Copy Step 2. We will reuse all of this.
 *      
 *  B.  Create a static class: MyThreadPool. In this,
 *      we will need a const or readonly static to track our
 *      thread count, and also a queue of work items to
 *      run on the thread pool. BlockingCollection<Action>
 *      is a good choice for the latter.
 *      
 *  C.  In the static constructor, create *Background*
 *      threads numbered according to the readonly field.
 *      Background threads are killed when the application exits.
 *      Each thread should have an indefinite loop,
 *      invoking the next item in the queue.
 *      
 *  D.  Add a static QueueUserWorkItem(Action action) method
 *      to add work items to the queue.
 *      
 *  E.  Start by modifying Run to run on the ThreadPool. However,
 *      note that we can no longer join the work to our
 *      working thread!
 *  
 *  F.  We will need a static counter and a ManualResetEvent
 *      because we can no longer Join the threads from the
 *      thread pool to our main thread. Initial state of
 *      the ManualResetEvent should be false.
 *      
 *  G.  Modify InstanceMethod to, at the end, decrement
 *      the counter thread-safe and if < 1, set the ManualResetEvent.
 *      
 *  H.  Update Run to wait on the ManualResetEvent at 
 *      the end instead of the Thread work we started with.
 *      
 * Async/await is built on top of the standard ThreadPool,
 * so gaining a basic understanding of it here is super
 * helpful. That is really the entire point of this sample.
 * 
 * =====================================================
*/

using System.Collections.Concurrent;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates creating a vey simple thread pool within C#. That's all
/// </summary>
public class MyThreadPoolSample : ITutorialSample
{
    /// <summary>
    /// A custom thread pool class. This just maintains a static pool of 2 threads.
    /// </summary>
    public static class MyThreadPool
    {
        /// <summary>
        /// The number of threads to have in the pool -- we start with 2 for demonstration
        /// </summary>
        private static readonly int _threadCount = 2;

        /// <summary>
        /// The collection of actions to be run on the pool
        /// </summary>
        private static readonly BlockingCollection<Action> _actionQueue = [];

        /// <summary>
        /// Static initializer for the thread pool, creates and launches the required threads
        /// </summary>
        static MyThreadPool()
        {
            // We just create the number of threads as Background threads so that they are killed when the application exits
            for (int i = 0; i < _threadCount; ++i)
            {
                new Thread(() =>
                {
                    // each thread just loops and when it is available, gets the next action on the worker queue and runs it
                    while (true)
                    {
                        _actionQueue.Take().Invoke();
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
            _actionQueue.Add(action);
        }
    }

    /// <summary>
    /// The number of actions to launch on the thread pool.
    /// We need this to coordinate when to finish because we no longer can join the threads!
    /// </summary>
    private static int _actionCount = 0;
    /// <summary>
    /// The reset event used to signal that all actions have completed processing
    /// We need this to coordinate when to finish because we no longer can join the threads!
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

        (int start, int end) = firstStart <= firstEnd ? (firstStart, firstEnd) : (firstEnd, firstStart);
        for (int value = start; value <= end; ++value)
        {
            Thread.Sleep(500);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }
        (start, end) = secondStart <= secondEnd ? (secondStart, secondEnd) : (secondEnd, secondStart);
        for (int value = start; value <= end; ++value)
        {
            Thread.Sleep(500);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");

        // Notify that we are finished, but only if we are the last thread to finish
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
        int actionCount = 5;
        // make sure we know how many times we need to decrement the global counter
        _actionCount = actionCount;
        for (int i = 0; i < _actionCount; ++i)
        {
            int mod = 10 * i;
            string identifier = $"Action {i}";
            // Instead of starting our own thread, launch on the thread pool!
            MyThreadPool.QueueUserWorkItem(() =>
                InstanceMethod(identifier,
                    1 + mod, 5 + mod,
                    10001 + mod, 10005 + mod));
        }

        // wait for the last thread to finish now.
        _resetEvent.Wait(cancellationToken);

        Console.WriteLine("All fin");
    }
}
