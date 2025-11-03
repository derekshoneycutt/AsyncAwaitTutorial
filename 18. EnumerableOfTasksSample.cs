using System.CommandLine;

namespace AsyncAwaitTutorial;


public static class EnumerableOfTasksSample
{
    public static async Task<int> DelayOnNumberAsync(int number, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        return number;
    }


    public static IEnumerable<Task<int>> GetDoubleNumbers(
        int firstStart, int firstMax, int secondStart, int secondMax,
        CancellationToken cancellationToken)
    {
        for (int value = firstStart; value <= firstMax; value++)
        {
            yield return DelayOnNumberAsync(value, cancellationToken);
        }
        for (int value = secondStart; value <= secondMax; value++)
        {
            yield return DelayOnNumberAsync(value, cancellationToken);
        }
    }


    public static async Task Run(
        ParseResult parseResult, CancellationToken cancellationToken)
    {
        IEnumerable<Task<int>> doubleNumberRunners =
            GetDoubleNumbers(1, 5, 101, 105, cancellationToken);

        foreach (Task<int> task in doubleNumberRunners)
        {
            int number = await task.ConfigureAwait(false);
            Console.WriteLine($"{Environment.CurrentManagedThreadId} => {number}");
        }
    }
}
