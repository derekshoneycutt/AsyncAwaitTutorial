/*
 * =====================================================
 *         Step 14 : Making an iterator with yield return
 * 
 *  Now we show how to write an iterator method using
 *  yield return, which the compiler translates into
 *  all the state machine stuff for us.
 *  
 *  
 *  A.  Copy Step 13. We will update this code.
 *  
 *  B.  Write a new method DoubleLoop that returns
 *      IEnumerable<int> and has 2 loops, inside each
 *      a Sleep and then yield return value.
 *  
 *  C.  Remove the manual IEnumerable classes and update
 *      InstanceMethod to call the new method instead.
 *      
 * This is a pretty simple step, but really brings home
 * the strength of some of these language features.
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates an IEnumerable iterator running over 2 loops
/// </summary>
/// <remarks>
/// Now we refactor the IEnumerable into the basic yield return format, returning to a much easier to read and understand code.
/// </remarks>
public class IteratorSample : ITutorialSample
{
    /// <summary>
    /// Returns an iterator that loops over 2 ranges of integers subsequently.
    /// </summary>
    /// <param name="firstStart">The first range start.</param>
    /// <param name="firstEnd">The first range maximum.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondEnd">The second range maximum.</param>
    /// <returns>An <see cref="IEnumerable{Int32}"/> that loops over 2 integer ranges subsequently.</returns>
    public static IEnumerable<int> DoubleLoop(
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        (int start, int end) = firstStart <= firstEnd ? (firstStart, firstEnd) : (firstEnd, firstStart);
        for (int value = start; value <= end; ++value)
        {
            Thread.Sleep(500);
            yield return value;
        }
        (start, end) = secondStart <= secondEnd ? (secondStart, secondEnd) : (secondEnd, secondStart);
        for (int value = start; value <= end; ++value)
        {
            Thread.Sleep(500);
            yield return value;
        }
    }

    /// <summary>
    /// The instance method to run as independent examples in the sample. This is a synchronous method.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The collection of values to print</param>
    public static void InstanceMethod(
        string identifier,
        IEnumerable<int> values)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        //List<int> listed = [.. values]; // Note the long delay that is the multiple Thread.Sleep occurring in this call!

        foreach (int value in values)
        {
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
        int actionCount = 5;
        for (int i = 0; i < actionCount; ++i)
        {
            int mod = 10 * i;
            string identifier = $"Action {i}";
            // Call the iterator here
            IEnumerable<int> values = DoubleLoop(
                1 + mod, 5 + mod,
                1001 + mod, 1005 + mod);
            InstanceMethod(identifier, values);
        }

        Console.WriteLine("All fin");
    }
}
