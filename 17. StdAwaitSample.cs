/*
 * =====================================================
 *         Step 17 : Standard async/await only
 * 
 *  We now know enough to just use the standard Task
 *  and async/await comfortably. We no longer have any
 *  need to use our custom Task structure.
 *  
 *  A.  Copy Step 16. We will update this code.
 *  
 *  B.  Remove the custom Task class and update all references
 *      to the standard Task class.
 *      We take some attention about ConfigureAwait now that
 *      it is available to us, recalling earlier discussion
 *      in our custom Thread Pool.
 *      
 *      
 * This is the final step so now everything we do will
 * be async/await!
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates async/await in standard for the first time
/// </summary>
public class StdAwaitSample : ITutorialSample
{
    // We don't need a custom Task type any more!

    /// <summary>
    /// Loops over 2 ranges of integers subsequently as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first range start.</param>
    /// <param name="firstEnd">The first range maximum.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondEnd">The second range maximum.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task InstanceMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        // Switch to normal Task.Delay
        // We also add .ConfigureAwait(false) to the end of any await call that we do not need to
        // return to the same execution context for. We would omit this if we are in the UI thread
        // and need to return back to the UI thread. However, it is recommended practice for all
        // non-UI related library code to use .ConfigureAwait(false) every time you use await!
        (int start, int end) = firstStart <= firstEnd ? (firstStart, firstEnd) : (firstEnd, firstStart);
        for (int value = start; value <= end; ++value)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }
        (start, end) = secondStart <= secondEnd ? (secondStart, secondEnd) : (secondEnd, secondStart);
        for (int value = start; value <= end; ++value)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(CancellationToken cancellationToken)
    {
        int actionCount = 55;
        List<Task> tasks = [];
        AsyncLocal<int> mod = new();
        for (int i = 0; i < actionCount; ++i)
        {
            mod.Value = 10 * i;
            string identifier = $"Action {i}";
            tasks.Add(
                InstanceMethod(identifier,
                    1 + mod.Value, 5 + mod.Value,
                    1001 + mod.Value, 1005 + mod.Value));
        }

        // even down here, we add the .ConfigureAwait(false)
        await Task.WhenAll(tasks).ConfigureAwait(false);

        Console.WriteLine("All fin");
    }
}
