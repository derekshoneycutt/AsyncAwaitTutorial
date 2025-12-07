namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates async/await in standard for the first time
/// </summary>
public class StdAwaitSample : ITutorialSample
{
    // We don't need a custom Task type any more!

    /// <summary>
    /// Loops over 2 ranges of integers subsequently as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first range start.</param>
    /// <param name="firstEnd">The first range maximum.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondEnd">The second range maximum.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task InstanceMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        // Switch to normal Task.Delay
        // We also add .ConfigureAwait(false) to the end of any await call that we do not need to
        // return to the same execution context for. We would omit this if we are in the UI thread
        // and need to return back to the UI thread. However, it is recommended practice for all
        // non-UI related library code to use .ConfigureAwait(false) every time you use await!
        for (int i = firstStart; i <= firstEnd; i++)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
        }
        for (int i = secondStart; i <= secondEnd; i++)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
        }

        Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");
    }


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(CancellationToken cancellationToken)
    {
        int actionCount = 55;
        List<Task> tasks = [];
        AsyncLocal<int> mod = new();
        for (int i = 0; i < actionCount; ++i)
        {
            mod.Value = 10 * i;
            string action = $"Action {i}";
            tasks.Add(
                InstanceMethod(action, 1 + mod.Value, 5 + mod.Value, 10001 + mod.Value, 10005 + mod.Value));
        }

        // even down here, we add the .ConfigureAwait(false)
        await Task.WhenAll(tasks).ConfigureAwait(false);

        Console.WriteLine("All fin");
    }
}
