/*
 * =====================================================
 *         Step 25 : IAsyncEnumerable Iterators
 * 
 *  Now we go back and use iterator methods instead of the whole
 *  custom implementation of the interfaces. The compiler will
 *  now do all that for us, and we get much cleaner, easier to
 *  read and maintain code.
 *  
 *  A.  Copy Step 24. We will update this code.
 *  
 *  B.  The easiest way to do this is copy FirstLoop and SecondLoop
 *      from Sample 23 and convert them into async IAsyncEnumerable
 *      that include the Delay, semaphore WaitAsync, and
 *      directly yield returns the value.
 *      The custom implementation can be removed with these in place.
 *      
 *  C.  Update Run to use the iterator methods instead of the custom
 *      implementations. This should be very easy.
 *      
 *      
 *  We now have a decoupled producer/consumer pattern in our code
 *  that makes it much easier to read and maintain.
 *  We do still have the same issue that Concat is not actually running
 *  our producers at the same time, however.
 * 
 * =====================================================
*/

using System.Runtime.CompilerServices;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates construction of an IAsyncEnumerable as an async iterator method
/// </summary>
public class IAsyncEnumerableGeneratorSample : ITutorialSample
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
    public static void DoubleLoop(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        TaskCompletionSource completionSource,
        CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

            for (int value = firstStart; value <= firstEnd; ++value)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
            }
            for (int value = secondStart; value <= secondEnd; ++value)
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
            completionSource.SetCanceled(cancellationToken);
        }
        catch (Exception ex)
        {
            completionSource.SetException(ex);
        }
    }

    /// <summary>
    /// Produces the specified ranges of values.
    /// </summary>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A list of the produced values</returns>
    public static async IAsyncEnumerable<int> Produce(
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int value = firstStart; value <= firstEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            yield return value;
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            yield return value;
        }
    }

    /// <summary>
    /// Consumes the collection, printing each value to the screen
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The values to print to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task Consume(
        string identifier,
        IAsyncEnumerable<int> values,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        List<Task> tasks = [];
        AsyncLocal<int> mod = new();
        for (int index = 1; index <= 55; ++index)
        {
            mod.Value = 10 * index;
            string identifier = $"Action {index}";
            // Update to the new async iterator implementation
            IAsyncEnumerable<int> values = Produce(
                1 + mod.Value, 5 + mod.Value,
                1001 + mod.Value, 1005 + mod.Value,
                cancellationToken);
            tasks.Add(Consume(identifier, values, cancellationToken));
        }

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            DoubleLoop("Single Thread",
                1, 5,
                101, 105,
                backThreadSource, cancellationToken)));
        instanceCaller.Start();
        tasks.Add(backThreadSource.Task);

        await Task.WhenAll(tasks).ConfigureAwait(false);

        Console.WriteLine("All fin");
    }
}
