/*
 * =====================================================
 *         Step 7 : Custom Thread Pool Sample
 * 
 *  This updates our code to run on a thread pool instead
 *  of launching individual threads. This actually takes us
 *  a step back as we place a severe limit on the number of
 *  threads in our pool, but this allows us to demonstrate
 *  the behavior of a thread pool and how we will utilize it.
 *  
 *  
 *  A.  We first create a new MyThreadPool static class
 *      that launches a static number of background threads
 *      and loops over a queue of work actions.
 *      
 *  B.  Create an action counter and reset event that will
 *      be used to signal when all of the tasks are complete.
 *      Update Consume to decrement the counter and signal
 *      the reset event if the counter goes below 1.
 *      
 *  C.  Update the Run method to launch Consume instances on
 *      the new thread pool class and wait for the reset event
 *      at the end.
 *      
 * We are now running on a thread pool that manages the threads
 * for us, instead of manually managing a thread for each instance
 * of Consume. This is an important step towards asynchrony,
 * but we are still concurrent at this point.
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
    private static ManualResetEventSlim _resetEvent = new(false);

    /// <summary>
    /// Produces the specified ranges of values.
    /// </summary>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    /// <returns>A list of the produced values</returns>
    public static IEnumerable<int> Produce(
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        for (int value = firstStart; value <= firstEnd; ++value)
        {
            Thread.Sleep(500);
            yield return value;
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            Thread.Sleep(500);
            yield return value;
        }
    }

    /// <summary>
    /// Consumes the collection, printing each value to the screen
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The values to print to the screen.</param>
    public static void Consume(
        string identifier,
        IEnumerable<int> values)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        foreach (int value in values)
        {
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
        // make sure we know how many times we need to decrement the global counter
        _actionCount = 5;
        _resetEvent = new(false);
        for (int index = 0; index <= 5; ++index)
        {
            int mod = 10 * index;
            string identifier = $"Action {index}";
            // Instead of starting our own thread, launch on the thread pool!
            MyThreadPool.QueueUserWorkItem(() =>
            {
                IEnumerable<int> values = Produce(
                    1 + mod, 5 + mod,
                    1001 + mod, 1005 + mod);
                Consume(identifier, values);
            });
        }

        // wait for the last thread to finish now.
        _resetEvent.Wait();

        Console.WriteLine("All fin");
    }
}
