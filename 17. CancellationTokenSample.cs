using System.CommandLine;

namespace AsyncAwaitTutorial;


public static class CancellationTokenSample
{
    public static void InstanceMethod(
        int firstStart, int firstMax, int secondStart, int secondMax,
        TaskCompletionSource completionSource,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Single Thread Writing values: {Environment.CurrentManagedThreadId}");

            for (int i = firstStart; i <= firstMax; i++)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"Single Thread: {i}");
            }
            for (int i = secondStart; i <= secondMax; i++)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"Single Thread: {i}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine("Fin Single Thread");

            completionSource.SetResult();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            completionSource.SetCanceled(cancellationToken);
        }
        catch (Exception ex)
        {
            completionSource.SetException(ex);
        }
    }

    public static async Task DoubleLoop(
        int firstStart, int firstMax, int secondStart, int secondMax,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Writing values: {Environment.CurrentManagedThreadId}");

        for (int value = firstStart; value <= firstMax; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"{Environment.CurrentManagedThreadId} => {value}");
        }
        for (int value = secondStart; value <= secondMax; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"{Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine("Fin");
    }


    public static async Task Run(ParseResult _, CancellationToken cancellationToken)
    {
        CancellationTokenSource cts = new();//new(3000);

        CancellationTokenSource linked =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

        int threadCount = 55;
        int mod;
        List<Task> tasks = [];
        for (int i = 0; i < threadCount; ++i)
        {
            mod = 10 * i;
            tasks.Add(
                DoubleLoop(1 + mod, 5 + mod, 10001 + mod, 10005 + mod,
                linked.Token));
        }

        await Task.Delay(1500, cancellationToken).ConfigureAwait(false);

        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            InstanceMethod(1, 5, 101, 105, backThreadSource, linked.Token)));
        instanceCaller.Start();
        tasks.Add(backThreadSource.Task);

        try
        {
            await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
            await cts.CancelAsync().ConfigureAwait(false);

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            Console.WriteLine("Canceled");
        }
        finally
        {
            instanceCaller.Join();
        }
    }
}
