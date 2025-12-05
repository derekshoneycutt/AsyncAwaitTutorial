namespace AsyncAwaitTutorial;



/// <summary>
/// This sample demonstrates an IEnumerable iterator running over 2 loops
/// </summary>
public static class IteratorSample
{
    /// <summary>
    /// Returns an iterator that loops over 2 ranges of integers subsequently.
    /// </summary>
    /// <param name="firstStart">The first range start.</param>
    /// <param name="firstMax">The first range maximum.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondMax">The second range maximum.</param>
    /// <returns>An <see cref="IEnumerable{Int32}"/> that loops over 2 integer ranges subsequently.</returns>
    public static IEnumerable<int> DoubleLoop(
        int firstStart, int firstMax, int secondStart, int secondMax)
    {
        for (int value = firstStart; value <= firstMax; ++value)
        {
            Thread.Sleep(1000);
            yield return value;
        }
        for (int value = secondStart; value <= secondMax; ++value)
        {
            Thread.Sleep(1000);
            yield return value;
        }
    }


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    public static void Run()
    {
        IEnumerable<int> myState = DoubleLoop(1, 5, 101, 105);

        //List<int> listed = [.. myState]; // Note the long delay that is the multiple Thread.Sleep occurring in this call!

        foreach (int value in myState)
        {
            Console.WriteLine(value);
        }
    }
}
