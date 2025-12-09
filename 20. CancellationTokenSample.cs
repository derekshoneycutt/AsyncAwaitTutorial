/*
 * =====================================================
 *         Step 20 : Standard Cancellation Token and Source
 * 
 *  Here we just replace our custom cancellation token with
 *  the real, standard thing, and show how it might change our
 *  code a little bit in some spots.
 *  
 *  A.  Copy Step 19. We will update this code.
 *  
 *  B.  Remove the definition of the custom cancellation token
 *      and source. Update all references to use the standard
 *      token instead.
 *      
 *  C.  Review existing async calls and add the cancellation
 *      tokens to the calls that don't already have one yet.
 *      If we want, we can use the constructor with a timeout
 *      in the standard cancellation token source as well.
 *      
 *      
 * There are some new places we can start using cancellation token
 * now that we're using the standard thing and passing it
 * everywhere. Additionally, we can remove some of the polls
 * for its canceled state, instead just letting the leaf methods
 * that we call poll it.
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates how to utilize CancellationToken to control asynchronous methods.
/// </summary>
public class CancellationTokenSample : ITutorialSample
{
    /// <summary>
    /// The instance method to run as independent threads in the sample. This is a synchronous method.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    /// <param name="completionSource">The Task Completion Source to mark when this task has completed</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static void ThreadMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        TaskCompletionSource completionSource,
        CancellationToken cancellationToken)
    {
        // Replace the parameter with the standard cancellation token type
        try
        {
            Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

            (int start, int end) = firstStart <= firstEnd ? (firstStart, firstEnd) : (firstEnd, firstStart);
            for (int value = start; value <= end; ++value)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
            }
            (start, end) = secondStart <= secondEnd ? (secondStart, secondEnd) : (secondEnd, secondStart);
            for (int value = start; value <= end; ++value)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");

            completionSource.SetResult();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // With the standard cancellation token, we can send the canceled token into our task completion source for more information to the caller
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
    /// <param name="firstEnd">The first range maximum.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondEnd">The second range maximum.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task InstanceMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        CancellationToken cancellationToken)
    {
        // Change to the standard cancellation token.
        // Send the cancellation token to the delays and remove any other polls on the token in this method--they're now superfluous
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        (int start, int end) = firstStart <= firstEnd ? (firstStart, firstEnd) : (firstEnd, firstStart);
        for (int value = start; value <= end; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }
        (start, end) = secondStart <= secondEnd ? (secondStart, secondEnd) : (secondEnd, secondStart);
        for (int value = start; value <= end; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        // We update to the standard cancellation token
        //  Note: We could use the constructor taking in a time limit instead of the manual delay below as well
        //   The function is largely the same, in that the token will cancel after a given timeout if specified.
        CancellationTokenSource cts = new();//new(4500);

        CancellationTokenSource linked =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

        CancellationTokenRegistration cancelRegister = linked.Token.Register(() =>
        {
            Console.WriteLine("Registered cancellation.");
        });

        int actionCount = 55;
        List<Task> tasks = [];
        AsyncLocal<int> mod = new();
        for (int i = 0; i < actionCount; ++i)
        {
            mod.Value = 10 * i;
            string action = $"Action {i}";
            // Add the cancellation token to the parameters
            tasks.Add(
                InstanceMethod(action,
                    1 + mod.Value, 5 + mod.Value,
                    1001 + mod.Value, 1005 + mod.Value,
                    cts.Token));
        }

        //We can pass the cancellation token down now that we know what to do!
        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            ThreadMethod("Single Thread",
                1, 5,
                101, 105,
                backThreadSource, linked.Token)));
        instanceCaller.Start();
        tasks.Add(backThreadSource.Task);

        try
        {
            await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
            // the standard cancel has an async cancel method
            // note: this one can cause problems with the UI thread in WPF, etc., so can't
            //   always be used, but it is nice when we can!
            await cts.CancelAsync().ConfigureAwait(false);

            await Task.WhenAll(tasks).ConfigureAwait(false);

            Console.WriteLine("All fin");
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            Console.WriteLine("Canceled");
        }
    }
}
