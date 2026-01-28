/*
 * =====================================================
 *         Step 5 : Iterator Producer Sample
 * 
 *  The point of this sample is to take our lessons from
 *  the state machines and IEnumerable and just use the
 *  build in Iterator Methods to make all of that code
 *  much nicer.
 *  
 *  
 *  A.  Remove the whole state machine/IEnumerable code
 *      and build a Produce method that loops through
 *      2 ranges, sleeping in each iteration and then
 *      yield return the current value.
 *      
 *  B.  Update the Run code to utilize the new Produce method.
 *      
 * This now gives us really nice, clean code that clearly
 * separates our production and consumption, at least
 * on the logical code level. This will be much easier
 * to work with moving forward and making it async.
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// Take the procedural starting sample and split the double loop into a Produce and Consume method pair.
/// </summary>
public class IteratorProducerSample : ITutorialSample
{
    /// <summary>
    /// Produces the specified ranges of values.
    /// </summary>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    /// <returns>A list of the produced values</returns>
    public static IEnumerable<int> Produce(
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        for (int value = firstStart; value <= firstEnd; ++value)
        {
            Thread.Sleep(500);
            yield return value;
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            Thread.Sleep(500);
            yield return value;
        }
    }

    /// <summary>
    /// Consumes the collection, printing each value to the screen
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The values to print to the screen.</param>
    public static void Consume(
        string identifier,
        IEnumerable<int> values)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        foreach (int value in values)
        {
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
        for (int index = 1; index <= 5; ++index)
        {
            int mod = 10 * index;
            string identifier = $"Action {index}";
            IEnumerable<int> values = Produce(
                1 + mod, 5 + mod,
                1001 + mod, 1005 + mod);
            Consume(identifier, values);
        }

        Console.WriteLine("All fin");
    }
}
