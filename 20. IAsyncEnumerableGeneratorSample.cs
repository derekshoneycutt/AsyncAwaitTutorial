using System.CommandLine;
using System.Runtime.CompilerServices;

namespace AsyncAwaitTutorial;



public static class IAsyncEnumerableGeneratorSample
{
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


    public static async Task Run(ParseResult parseResult, CancellationToken cancellationToken)
    {
        IAsyncEnumerable<int> myState = DoubleLoop(1, 5, 101, 105, cancellationToken);

        await foreach (int value in myState.ConfigureAwait(false))
        {
            Console.WriteLine(value);
        }
    }
}
