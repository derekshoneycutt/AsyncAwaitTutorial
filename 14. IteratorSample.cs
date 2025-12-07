namespace AsyncAwaitTutorial;



/// <summary>
/// This sample demonstrates an IEnumerable iterator running over 2 loops
/// </summary>
/// <remarks>
/// Now we refactor the IEnumerable into the basic yield return format, returning to a much easier to read and understand code.
/// </remarks>
public class IteratorSample : ITutorialSample
{
    /// <summary>
    /// Returns an iterator that loops over 2 ranges of integers subsequently.
    /// </summary>
    /// <param name="firstStart">The first range start.</param>
    /// <param name="firstEnd">The first range maximum.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondEnd">The second range maximum.</param>
    /// <returns>An <see cref="IEnumerable{Int32}"/> that loops over 2 integer ranges subsequently.</returns>
    public static IEnumerable<int> DoubleLoop(
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
    /// The instance method to run as independent examples in the sample. This is a synchronous method.
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

        IEnumerable<int> myState = DoubleLoop(1, 5, 101, 105);

        //List<int> listed = [.. myState]; // Note the long delay that is the multiple Thread.Sleep occurring in this call!

        foreach (int value in myState)
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(CancellationToken cancellationToken)
    {
        int actionCount = 5;
        for (int i = 0; i < actionCount; ++i)
        {
            int mod = 10 * i;
            string action = $"Action {i}";
            InstanceMethod(action, 1 + mod, 5 + mod, 1001 + mod, 1005 + mod);
        }

        Console.WriteLine("All fin");
    }
}
