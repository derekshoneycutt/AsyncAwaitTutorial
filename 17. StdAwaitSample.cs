/*
 * =====================================================
 *         Step 17 : Standard async/await only
 * 
 *  We now know enough to just use the standard Task
 *  and async/await comfortably. We no longer have any
 *  need to use our custom Task structure.
 *  
 *      Remove the custom Task class and update all references
 *      to the standard Task class.
 *      This will require making a special DelayOnNumber method
 *      to handle the async in Produce for now.
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
    // We don't need a custom Task type any more! But we do need a new DelayOnNumber because we don't know how to do otherwise yet

    /// <summary>
    /// Delays for a second and then returns a given number as an asynchronous operation.
    /// </summary>
    /// <param name="number">The number to return.</param>
    /// <returns>A <see cref="Task{Int32}"/> that represents the asynchronous operation. <c>Result</c> contains the specified integer.</returns>
    public static async Task<int> DelayOnNumber(
        int number)
    {
        // Switch to normal Task.Delay
        // We also add .ConfigureAwait(false) to the end of any await call that we do not need to
        // return to the same execution context for. We would omit this if we are in the UI thread
        // and need to return back to the UI thread. However, it is recommended practice for all
        // non-UI related library code to use .ConfigureAwait(false) every time you use await!
        await Task.Delay(1000).ConfigureAwait(false);
        return number;
    }

    /// <summary>
    /// Produces the specified ranges of values.
    /// </summary>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    /// <returns>A list of the produced values</returns>
    public static IEnumerable<Task<int>> Produce(
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        for (int value = firstStart; value <= firstEnd; ++value)
        {
            yield return DelayOnNumber(value);
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            yield return DelayOnNumber(value);
        }
    }

    /// <summary>
    /// Consumes the collection, printing each value to the screen
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The values to print to the screen.</param>
    public static async Task Consume(
        string identifier,
        IEnumerable<Task<int>> values)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        foreach (Task<int> valueTask in values)
        {
            int value = await valueTask.ConfigureAwait(false);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(CancellationToken cancellationToken)
    {
        List<Task> tasks = [];
        AsyncLocal<int> mod = new();
        for (int index = 1; index <= 55; ++index)
        {
            mod.Value = 10 * index;
            string identifier = $"Action {index}";
            IEnumerable<Task<int>> values = Produce(
                1 + mod.Value, 5 + mod.Value,
                1001 + mod.Value, 1005 + mod.Value);
            tasks.Add(Consume(identifier, values));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        Console.WriteLine("All fin");
    }
}
