namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates launching threads within C#. That's all
/// <para>
/// This launches 4 threads, each of which is the InstanceMethod with different values.
/// The threads run through 2 loops, printing each number in the specified ranges.
/// </para>
/// </summary>
public class ThreadSample : ITutorialSample
{
    /// <summary>
    /// The instance method to run as independent threads in the sample. This is a synchronous method.
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

        for (int i = firstStart; i <= firstEnd; i++)
        {
            Thread.Sleep(500);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
        }
        for (int i = secondStart; i <= secondEnd; i++)
        {
            Thread.Sleep(500);
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
        int actionCount = 5;
        List<Thread> threads = []; // Store threads spun off here
        for (int i = 0; i < actionCount; ++i)
        {
            int mod = 10 * i;
            string action = $"Action {i}";
            // Create and start a thread, adding it to the collection
            Thread thread = new(new ThreadStart(() => InstanceMethod(action, 1 + mod, 5 + mod, 1001 + mod, 1005 + mod)));
            thread.Start();
            threads.Add(thread);
        }

        // Join all the stored threads to the current before finishing.
        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        Console.WriteLine("All fin");
    }
}
