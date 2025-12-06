namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates how to utilize CancellationToken to control asynchronous methods.
/// </summary>
public static class CancellationTokenSample
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
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static void InstanceMethod(
        string identifier,
        int firstStart, int firstMax, int secondStart, int secondMax,
        TaskCompletionSource completionSource,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

            for (int i = firstStart; i <= firstMax; i++)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
            }
            for (int i = secondStart; i <= secondMax; i++)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");

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


    /// <summary>
    /// Loops over 2 ranges of integers subsequently as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first range start.</param>
    /// <param name="firstMax">The first range maximum.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondMax">The second range maximum.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task DoubleLoop(
        string identifier,
        int firstStart, int firstMax, int secondStart, int secondMax,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        for (int i = firstStart; i <= firstMax; i++)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
        }
        for (int i = secondStart; i <= secondMax; i++)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
        }

        Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");
    }


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task Run(
        CancellationToken cancellationToken)
    {
        CancellationTokenSource cts = new();//new(4500);

        CancellationTokenSource linked =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

        CancellationTokenRegistration cancelRegister = linked.Token.Register(() =>
        {
            Console.WriteLine("Registered cancellation.");
        });

        int threadCount = 55;
        List<Task> tasks = [];
        for (int i = 0; i < threadCount; ++i)
        {
            int mod = 10 * i;
            string action = $"Action {i}";
            tasks.Add(
                DoubleLoop(action, 1 + mod, 5 + mod, 10001 + mod, 10005 + mod, linked.Token));
        }

        await Task.Delay(1500, cancellationToken).ConfigureAwait(false);

        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            InstanceMethod("Single Thread", 1, 5, 101, 105, backThreadSource, linked.Token)));
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
    }
}
