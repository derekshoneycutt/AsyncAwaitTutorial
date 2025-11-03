namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates how to utilize a TaskCompletionSource to expand asynchronous code
/// </summary>
public static class TaskCompletionSourceSample
{
    /// <summary>
    /// The instance method to run as independent threads in the sample. This is a synchronous method.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstMax">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondMax">The second maximum value, completing the second range.</param>
    /// <param name="completionSource">The Task Completion Source to mark when this task has completed</param>
    public static void InstanceMethod(
        string identifier,
        int firstStart, int firstMax, int secondStart, int secondMax,
        TaskCompletionSource completionSource)
    {
        try
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

            completionSource.SetResult();
        }
        catch (Exception ex)
        {
            completionSource.SetException(ex);
        }
    }


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

        await Task.Delay(500).ConfigureAwait(false);

        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            InstanceMethod("Single Thread", 1, 5, 101, 105, backThreadSource)));
        instanceCaller.Start();
        tasks.Add(backThreadSource.Task);

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
