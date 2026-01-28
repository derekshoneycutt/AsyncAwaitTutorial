namespace AsyncAwaitTutorial;

/// <summary>
/// Samples setting the basic zero-basis, not even using threads or anything really.
/// </summary>
public class ProceduralSample : ITutorialSample
{
    /// <summary>
    /// The instance method to run as independent examples in the sample. This is a synchronous method.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    public static void DoubleLoop(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        for (int value = firstStart; value <= firstEnd; ++value)
        {
            Thread.Sleep(500);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            Thread.Sleep(500);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(CancellationToken cancellationToken)
    {
        // Run the sample a variable number of times -- low here because we're synchronous and in series
        for (int index = 1; index <= 5; ++index)
        {
            int mod = 10 * index;
            string identifier = $"Action {index}";
            DoubleLoop(identifier,
                1 + mod, 5 + mod,
                1001 + mod, 1005 + mod);
        }

        Console.WriteLine("All fin");
    }
}
