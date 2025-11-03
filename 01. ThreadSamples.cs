using System.CommandLine;

namespace AsyncAwaitTutorial;



public static class ThreadSamples
{
    public static void InstanceMethod(
        string identifier,
        int firstStart, int firstMax, int secondStart, int secondMax)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        for (int i = firstStart; i <= firstMax; i++)
        {
            Thread.Sleep(1000);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
        }
        for (int i = secondStart; i <= secondMax; i++)
        {
            Thread.Sleep(1000);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
        }

        Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");
    }


    public static void Run(ParseResult parseResult)
    {
        Thread instanceCaller = new(new ThreadStart(() => InstanceMethod("Thread 1", 1, 5, 101, 105)));
        instanceCaller.Start();

        Thread instanceCaller2 = new(new ThreadStart(() => InstanceMethod("Thread 2", 11, 15, 111, 115)));
        instanceCaller2.Start();

        Thread instanceCaller3 = new(new ThreadStart(() => InstanceMethod("Thread 3", 21, 25, 121, 125)));
        instanceCaller3.Start();

        Thread instanceCaller4 = new(new ThreadStart(() => InstanceMethod("Thread 4", 31, 35, 131, 135)));
        instanceCaller4.Start();

        instanceCaller.Join();
        instanceCaller2.Join();
        instanceCaller3.Join();
        instanceCaller4.Join();

        Console.WriteLine("All fin");
    }
}
