using System.Collections.Concurrent;

namespace AsyncAwaitTutorial;




/// <summary>
/// This sample demonstrates adding ExecutionContext to the custom thread pool made in the previous sample. That's all
/// <para>
/// This launches threads counted by the number of processor count (changed from just 2!)
/// in a pool and balances multiple actions queued into the pool.
/// </para>
/// </summary>
public static class MyThreadPoolWithContextSamples
{

    /// <summary>
    /// A custom thread pool class. This just maintains a static pool of 2 threads.
    /// </summary>
    public static class MyThreadPool
    {
        /// <summary>
        /// The number of threads to have in the pool
        /// </summary>
        private static readonly int _threadCount = Environment.ProcessorCount;


        /// <summary>
        /// The collection of actions to be run on the pool
        /// </summary>
        public static readonly BlockingCollection<(Action, ExecutionContext?)> _actionQueue = [];


        /// <summary>
        /// Static initializer for the thread pool, creates and launches the required threads
        /// </summary>
        static MyThreadPool()
        {
            for (int i = 0; i < _threadCount; ++i)
            {
                new Thread(() =>
                {
                    int iterCount = 0;
                    while (true)
                    {
                        Console.WriteLine($"Thread iter {++iterCount}");
                        (Action nextAction, ExecutionContext? context) = _actionQueue.Take();
                        if (context is null)
                        {
                            nextAction();
                        }
                        else
                        {
                            ExecutionContext.Run(context, act => ((Action)act!).Invoke(), nextAction);
                        }
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
    /// <param name="firstMax">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondMax">The second maximum value, completing the second range.</param>
    public static void InstanceMethod(
        string identifier,
        int firstStart, int firstMax, int secondStart, int secondMax)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        for (int i = firstStart; i <= firstMax; i++)
        {
            Thread.Sleep(1000);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
        }
        for (int i = secondStart; i <= secondMax; i++)
        {
            Thread.Sleep(1000);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
        }

        Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");

        if (Interlocked.Decrement(ref _actionCount) < 1)
        {
            _resetEvent.Set();
        }
    }


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    public static void Run()
    {
        _actionCount = 55;
        AsyncLocal<int> mod = new();
        for (int i = 0; i < _actionCount; ++i)
        {
            mod.Value = 10 * i;
            string action = $"Action {i}";
            MyThreadPool.QueueUserWorkItem(() => InstanceMethod(
                action, 1 + mod.Value, 5 + mod.Value, 10001 + mod.Value, 10005 + mod.Value));
        }

        _resetEvent.Wait();

        Console.WriteLine("All fin");
    }
}
