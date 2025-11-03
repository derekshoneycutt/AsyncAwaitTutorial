using System.CommandLine;

namespace AsyncAwaitTutorial;


public static class StdTaskAtContinueWithSample
{
    public static Task InstanceMethod(
        int firstStart, int firstMax, int secondStart, int secondMax)
    {
        int i = firstStart;
        Task IncrementAndPrint(int max)
        {
            Console.WriteLine(i);
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
            Console.WriteLine($"Writing values: {Environment.CurrentManagedThreadId}");

            return Task.Delay(1000)
                .ContinueWith(_ => IncrementAndPrint(firstMax)
                    .ContinueWith(_ => Task.Delay(1000)
                        .ContinueWith(_ =>
                        {
                            i = secondStart;
                            return IncrementAndPrint(secondMax)
                                .ContinueWith(_ => Console.WriteLine("Fin"));
                        }).Unwrap()).Unwrap()).Unwrap();
        });
    }


    public static void Run(ParseResult parseResult)
    {
        int threadCount = 55;
        AsyncLocal<int> mod = new();
        List<Task> tasks = [];
        for (int i = 0; i < threadCount; ++i)
        {
            mod.Value = 10 * i;
            tasks.Add(
                InstanceMethod(1 + mod.Value, 5 + mod.Value, 10001 + mod.Value, 10005 + mod.Value));
        }

        Task.WhenAll(tasks).Wait();
    }
}
