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
            Thread.Sleep(1000);
            yield return value;
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            Thread.Sleep(1000);
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
        _actionCount = 55;
        _resetEvent = new(false);
        AsyncLocal<int> mod = new();
        for (int index = 1; index <= 55; ++index)
        {
            mod.Value = 10 * index;
            string identifier = $"Action {index}";
            // Move to the standard ThreadPool instead; performance optimizations exist here.
            ThreadPool.QueueUserWorkItem<ThreadPoolState>(state =>
            {
                IEnumerable<int> values = Produce(
                    1 + state.Mod.Value, 5 + state.Mod.Value,
                    1001 + state.Mod.Value, 1005 + state.Mod.Value);
                Consume(state.Identifier, values);
            }, new(identifier, mod), true);
        }

        _resetEvent.Wait();

        Console.WriteLine("All fin");
    }
}

