namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates an enumerable of tasks to introduce the logic of IAsyncEnumerable
/// </summary>
public static class EnumerableOfTasksSample
{
    /// <summary>
    /// Delays for a second and then returns a given number as an asynchronous operation.
    /// </summary>
    /// <param name="number">The number to return.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A <see cref="Task{Int32}"/> that represents the asynchronous operation. <c>Result</c> contains the specified integer.</returns>
    public static async Task<int> DelayOnNumberAsync(int number, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        return number;
    }


    /// <summary>
    /// Loops through 2 ranges of numbers subsequently, returning a task for each one that gives the number back after a delay
    /// </summary>
    /// <param name="firstStart">The first range start.</param>
    /// <param name="firstMax">The first range maximum.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondMax">The second range maximum.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>An enumerable of tasks representing the asynchronous operations for each number.</returns>
    /// <remarks>
    /// This looks remarkably similar to the simulated async/await using yield return that was simplified before.....
    /// </remarks>
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


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task Run(
        CancellationToken cancellationToken)
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
