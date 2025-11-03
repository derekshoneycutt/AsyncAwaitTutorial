using System.Runtime.CompilerServices;

namespace AsyncAwaitTutorial;



/// <summary>
/// This sample demonstrates construction of an IAsyncEnumerable as an async iterator method
/// </summary>
public static class IAsyncEnumerableGeneratorSample
{
    /// <summary>
    /// Asynchonously iterates over 2 ranges subsequently, with a delay between each iteration
    /// </summary>
    /// <param name="firstStart">The first range start.</param>
    /// <param name="firstMax">The first range maximum.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondMax">The second range maximum.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Int32}"/> that iterates over each item in the 2 ranges, with a delay between each iteration.</returns>
    public static async IAsyncEnumerable<int> DoubleLoop(
        int firstStart, int firstMax, int secondStart, int secondMax,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int value = firstStart; value <= firstMax; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            yield return value;
        }
        for (int value = secondStart; value <= secondMax; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            yield return value;
        }
    }


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task Run(CancellationToken cancellationToken)
    {
        IAsyncEnumerable<int> myState = DoubleLoop(1, 5, 101, 105, cancellationToken);

        await foreach (int value in myState.ConfigureAwait(false))
        {
            Console.WriteLine(value);
        }
    }
}
