namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates async/await in standard for the first time
/// </summary>
public static class StdAwaitSample
{

    /// <summary>
    /// Loops over 2 ranges of integers subsequently as an asynchronous operaiton
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first range start.</param>
    /// <param name="firstMax">The first range maximum.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondMax">The second range maximum.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task DoubleLoop(
        string identifier,
        int firstStart, int firstMax, int secondStart, int secondMax)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        for (int i = firstStart; i <= firstMax; i++)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
        }
        for (int i = secondStart; i <= secondMax; i++)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
        }

        Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");
    }


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    public static async Task Run()
    {
        int threadCount = 55;
        List<Task> tasks = [];
        for (int i = 0; i < threadCount; ++i)
        {
            int mod = 10 * i;
            string action = $"Action {i}";
            tasks.Add(
                DoubleLoop(action, 1 + mod, 5 + mod, 10001 + mod, 10005 + mod));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
