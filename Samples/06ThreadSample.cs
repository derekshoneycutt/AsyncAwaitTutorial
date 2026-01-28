/*
 * =====================================================
 *         Step 2 : Thread Sample
 * 
 *  This launches multiple threads, each of which is the
 *  Consume method with different values.
 *  
 *  
 *  A.  Add a List<Thread> and launch each call to
 *      Consume as a Thread, adding it to the list.
 *      
 *  B.  After the initial loop, loop through the list
 *      of threads and Join them, effectively waiting
 *      for each to finish.
 *      
 * This is just the first step to show we can do
 * concurrency and make sure we have a basic idea of what
 * is going on under the hood. We'll also manage
 * a thread a lot in the future this way.
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates launching threads within C#. That's all
/// </summary>
public class ThreadSample : ITutorialSample
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
        List<Thread> threads = []; // Store threads spun off here
        for (int index = 1; index <= 5; ++index)
        {
            int mod = 10 * index;
            string identifier = $"Action {index}";
            // Create and start a thread, adding it to the collection
            Thread thread = new(new ThreadStart(() =>
            {
                IEnumerable<int> values = Produce(
                    1 + mod, 5 + mod,
                    1001 + mod, 1005 + mod);
                Consume(identifier, values);
            }));
            thread.Start();
            threads.Add(thread);
        }

        // Join all the stored threads to the current before finishing.
        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        Console.WriteLine("All fin");
    }
}
