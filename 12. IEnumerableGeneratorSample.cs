using System.CommandLine;

namespace AsyncAwaitTutorial;



public static class IEnumerableGeneratorSample
{
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


    public static void Run(ParseResult parseResult)
    {
        IEnumerable<int> myState = DoubleLoop(1, 5, 101, 105);

        //List<int> listed = [.. myState]; // Same as below...

        foreach (int value in myState)
        {
            Console.WriteLine(value);
        }
    }
}
