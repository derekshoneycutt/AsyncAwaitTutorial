namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates launching threads within C#. That's all
/// <para>
/// This launches 4 threads, each of which is the InstanceMethod with different values.
/// The theads run through 2 loops, printing each number in the specified ranges.
/// </para>
/// </summary>
public static class ThreadSamples
{
    /// <summary>
    /// The instance method to run as independent threads in the sample. This is a synchronous method.
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
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    public static void Run()
    {
        Thread instanceCaller = new(new ThreadStart(() => InstanceMethod("Thread 1", 1, 5, 101, 105)));
        instanceCaller.Start();

        Thread instanceCaller2 = new(new ThreadStart(() => InstanceMethod("Thread 2", 11, 15, 111, 115)));
        instanceCaller2.Start();

        Thread instanceCaller3 = new(new ThreadStart(() => InstanceMethod("Thread 3", 21, 25, 121, 125)));
        instanceCaller3.Start();

        Thread instanceCaller4 = new(new ThreadStart(() => InstanceMethod("Thread 4", 31, 35, 131, 135)));
        instanceCaller4.Start();

        instanceCaller.Join();
        instanceCaller2.Join();
        instanceCaller3.Join();
        instanceCaller4.Join();

        Console.WriteLine("All fin");
    }
}
