using System.CommandLine;

namespace AsyncAwaitTutorial;


public static class StdAwaitSample
{

    public static async Task DoubleLoop(
        int firstStart, int firstMax, int secondStart, int secondMax)
    {
        Console.WriteLine($"Writing values: {Environment.CurrentManagedThreadId}");

        for (int value = firstStart; value <= firstMax; ++value)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            Console.WriteLine($"{Environment.CurrentManagedThreadId} => {value}");
        }
        for (int value = secondStart; value <= secondMax; ++value)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            Console.WriteLine($"{Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine("Fin");
    }


    public static async Task Run(ParseResult parseResult, CancellationToken cancellationToken)
    {
        int threadCount = 55;
        AsyncLocal<int> mod = new();
        List<Task> tasks = [];
        for (int i = 0; i < threadCount; ++i)
        {
            mod.Value = 10 * i;
            tasks.Add(
                DoubleLoop(1 + mod.Value, 5 + mod.Value, 10001 + mod.Value, 10005 + mod.Value));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
