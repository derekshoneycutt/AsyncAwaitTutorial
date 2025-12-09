/*
 * =====================================================
 *         Step 1 : Procedural Sample
 * 
 *  The point of this sample is to show that we are running 2 loops,
 *  printing the values, with a delay between each printing.
 *  This is the basic idea of what we are trying to do in an
 *  asynchronous way with future samples.
 *  
 *  
 *  A.  Create InstanceMethod: take an identifier and 2 start/end sets
 *      print start, loop start to end in each, printing values, then print end.
 *      
 *  B.  Run can be implemented to call InstanceMethod subsequently
 *      a variable (set to 5?) number of times, one after the other
 *      
 * This really just gives us a logical basis for the procedure
 * we want to do more asynchronously moving forward.
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// Samples setting the basic zero-basis, not even using threads or anything really.
/// </summary>
public class ProceduralSample : ITutorialSample
{
    /// <summary>
    /// The instance method to run as independent examples in the sample. This is a synchronous method.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    public static void InstanceMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        (int start, int end) = firstStart <= firstEnd ? (firstStart, firstEnd) : (firstEnd, firstStart);
        for (int value = start; value <= end; ++value)
        {
            Thread.Sleep(500);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }
        (start, end) = secondStart <= secondEnd ? (secondStart, secondEnd) : (secondEnd, secondStart);
        for (int value = start; value <= end; ++value)
        {
            Thread.Sleep(500);
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
        // Run the sample a variable number of times -- low here because we're synchronous and in series
        int actionCount = 5;
        for (int i = 0; i < actionCount; ++i)
        {
            int mod = 10 * i;
            string identifier = $"Action {i + 1}";
            InstanceMethod(identifier,
                1 + mod, 5 + mod,
                1001 + mod, 1005 + mod);
        }

        Console.WriteLine("All fin");
    }
}
