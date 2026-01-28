/*
 * =====================================================
 *         Step 2 : List Producer Sample
 * 
 *  The point of this sample is to split the basic procedural
 *  sample that we started with into a pair of Produce and Consume
 *  methods, which will help us demonstrate important patterns
 *  and language tools as we progress.
 *  
 *  
 *  A.  Split off a Produce method that loops through 2 ranges,
 *      adding the values to a list that is returned at the end.
 *      
 *  B.  Make a consume method that loops through a collection of
 *      values and prints them to the screen.
 *      
 *  C.  Update Run to call Produce and Consume.
 *      
 * This separates our production of values from the consumption
 * and display of values. This is a great win for maintainable code,
 * but highlights some limitations to such a simple approach.
 * Namely, the Sleep has to be in the Consume method, and we
 * cannot consume values as they are produced--we only consume
 * values once the production has fully completed.
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// Take the procedural starting sample and split the double loop into a Produce and Consume method pair.
/// </summary>
public class ListProducerSample : ITutorialSample
{
    /// <summary>
    /// Produces the specified ranges of values.
    /// </summary>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    /// <returns>A list of the produced values</returns>
    public static List<int> Produce(
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        List<int> values = [];

        for (int value = firstStart; value <= firstEnd; ++value)
        {
            values.Add(value);
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            values.Add(value);
        }

        return values;
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
        for (int index = 1; index <= 5; ++index)
        {
            int mod = 10 * index;
            string identifier = $"Action {index}";
            List<int> values = Produce(
                1 + mod, 5 + mod,
                1001 + mod, 1005 + mod);
            Consume(identifier, values);
        }

        Console.WriteLine("All fin");
    }
}
