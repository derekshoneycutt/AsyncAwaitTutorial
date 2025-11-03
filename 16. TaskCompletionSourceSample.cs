using System.CommandLine;

namespace AsyncAwaitTutorial;


public static class TaskCompletionSourceSample
{
    public static void InstanceMethod(
        int firstStart, int firstMax, int secondStart, int secondMax,
        TaskCompletionSource completionSource)
    {
        try
        {
            Console.WriteLine($"Single Thread Writing values: {Environment.CurrentManagedThreadId}");

            for (int i = firstStart; i <= firstMax; i++)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"Single Thread: {i}");
            }
            for (int i = secondStart; i <= secondMax; i++)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"Single Thread: {i}");
            }

            Console.WriteLine("Fin Single Thread");

            completionSource.SetResult();
        }
        catch (Exception ex)
        {
            completionSource.SetException(ex);
        }
    }



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

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);

        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            InstanceMethod(1, 5, 101, 105, backThreadSource)));
        instanceCaller.Start();
        tasks.Add(backThreadSource.Task);

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        finally
        {
            instanceCaller.Join();
        }
    }
}
