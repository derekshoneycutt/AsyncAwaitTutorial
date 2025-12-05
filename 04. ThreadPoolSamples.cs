namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates using the standard ThreadPool class. That's all
/// </summary>
public static class ThreadPoolSamples
{
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
        for (int i = 0; i < _actionCount; ++i)
        {
            int mod = 10 * i;
            string action = $"Action {i}";
            ThreadPool.QueueUserWorkItem(_ => InstanceMethod(
                action, 1 + mod, 5 + mod, 10001 + mod, 10005 + mod));
        }

        _resetEvent.Wait();

        Console.WriteLine("All fin");
    }
}

