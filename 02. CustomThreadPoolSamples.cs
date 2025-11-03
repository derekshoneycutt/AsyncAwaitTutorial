using System.Collections.Concurrent;
using System.CommandLine;

namespace AsyncAwaitTutorial;


public static class MyThreadPool
{
    private static readonly int _threadCount = 2;// Environment.ProcessorCount;


    public static readonly BlockingCollection<Action> _actionQueue = [];


    static MyThreadPool()
    {
        for (int i = 0; i < _threadCount; ++i)
        {
            new Thread(() =>
            {
                while (true)
                {
                    Action nextAction = _actionQueue.Take();
                    nextAction();
                }
            })
            { IsBackground = true }.Start();
        }
    }



    public static void QueueUserWorkItem(Action action)
    {
        _actionQueue.Add(action);
    }
}


public static class CustomThreadPoolSamples
{
    private static int _threadCount = 0;
    private static readonly ManualResetEventSlim _resetEvent = new(false);

    public static void InstanceMethod(
        int firstStart, int firstMax, int secondStart, int secondMax)
    {
        Console.WriteLine($"Writing values: {Environment.CurrentManagedThreadId}");

        for (int i = firstStart; i <= firstMax; i++)
        {
            Thread.Sleep(1000);
            Console.WriteLine(i);
        }
        for (int i = secondStart; i <= secondMax; i++)
        {
            Thread.Sleep(1000);
            Console.WriteLine(i);
        }

        Console.WriteLine("Fin");

        if (Interlocked.Decrement(ref _threadCount) < 1)
        {
            _resetEvent.Set();
        }
    }


    public static void Run(ParseResult parseResult)
    {
        _threadCount = 5;
        for (int i = 0; i < _threadCount; ++i)
        {
            int mod = 10 * i;
            MyThreadPool.QueueUserWorkItem(() => InstanceMethod(1 + mod, 5 + mod, 10001 + mod, 10005 + mod));
        }

        _resetEvent.Wait();

        Console.WriteLine("All fin");
    }
}
