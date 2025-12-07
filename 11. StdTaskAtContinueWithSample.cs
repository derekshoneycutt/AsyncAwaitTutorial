namespace AsyncAwaitTutorial;



/// <summary>
/// This sample demonstrates creating an asynchronous chain of work utilizing the standard Tasks
/// </summary>
public class StdTaskAtContinueWithSample : ITutorialSample
{
    /// <summary>
    /// The instance method to run as tasks.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    public static Task InstanceMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        // Refactor this to use just the standard Task
        // Note the addition of .Unwrap() in multiple places because standard Task returns Task<Task> instead of folding it as we have done in the previous samples
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
                .ContinueWith(_ => IncrementAndPrint(firstEnd)
                    .ContinueWith(_ => Task.Delay(1000)
                        .ContinueWith(_ =>
                        {
                            i = secondStart;
                            return IncrementAndPrint(secondEnd)
                                .ContinueWith(_ => Console.WriteLine(
                                    $"Fin  {identifier} / {Environment.CurrentManagedThreadId}"));
                        }).Unwrap()).Unwrap()).Unwrap();
        });
    }



    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(CancellationToken cancellationToken)
    {
        int actionCount = 55;
        // Everything in Run must be updated to the standard Task as well
        List<Task> tasks = [];
        AsyncLocal<int> mod = new();
        for (int i = 0; i < actionCount; ++i)
        {
            mod.Value = 10 * i;
            string action = $"Action {i}";
            tasks.Add(InstanceMethod(
                action, 1 + mod.Value, 5 + mod.Value, 10001 + mod.Value, 10005 + mod.Value));
        }

        Task.WhenAll(tasks).Wait(cancellationToken);

        Console.WriteLine("All fin");
    }
}
