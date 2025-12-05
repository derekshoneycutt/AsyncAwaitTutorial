namespace AsyncAwaitTutorial;



/// <summary>
/// This sample demonstrates creating an asynchronous chain of work utilizing the standard Tasks
/// </summary>
public static class StdTaskAtContinueWithSample
{
    /// <summary>
    /// The instance method to run as tasks.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstMax">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondMax">The second maximum value, completing the second range.</param>
    public static Task InstanceMethod(
        string identifier,
        int firstStart, int firstMax, int secondStart, int secondMax)
    {
        int i = firstStart;
        Task IncrementAndPrint(int max)
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
            ++i;

            if (i <= max)
            {
                return Task.Delay(1000)
                    .ContinueWith(_ => IncrementAndPrint(max))
                    .Unwrap();
            }

            return Task.CompletedTask;
        }

        return Task.Run(() =>
        {
            Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

            return Task.Delay(1000)
                .ContinueWith(_ => IncrementAndPrint(firstMax)
                    .ContinueWith(_ => Task.Delay(1000)
                        .ContinueWith(_ =>
                        {
                            i = secondStart;
                            return IncrementAndPrint(secondMax)
                                .ContinueWith(_ => Console.WriteLine(
                                    $"Fin  {identifier} / {Environment.CurrentManagedThreadId}"));
                        }).Unwrap()).Unwrap()).Unwrap();
        });
    }



    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    public static void Run()
    {
        int threadCount = 55;
        List<Task> tasks = [];
        for (int i = 0; i < threadCount; ++i)
        {
            int mod = 10 * i;
            string action = $"Action {i}";
            tasks.Add(InstanceMethod(
                action, 1 + mod, 5 + mod, 10001 + mod, 10005 + mod));
        }

        Task.WhenAll(tasks).Wait();
    }
}
