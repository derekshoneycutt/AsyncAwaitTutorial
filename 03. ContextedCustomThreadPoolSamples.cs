using System.Collections.Concurrent;
using System.CommandLine;

namespace AsyncAwaitTutorial;


public static class MyThreadPoolWithContext
{
    private static readonly int _threadCount = Environment.ProcessorCount;


    public static readonly BlockingCollection<(Action, ExecutionContext?)> _actionQueue = [];


    static MyThreadPoolWithContext()
    {
        for (int i = 0; i < _threadCount; ++i)
        {
            new Thread(() =>
            {
                int iterCount = 0;
                while (true)
                {
                    Console.WriteLine($"Thread iter {++iterCount}");
                    (Action nextAction, ExecutionContext? context) = _actionQueue.Take();
                    if (context is null)
                    {
                        nextAction();
                    }
                    else
                    {
                        ExecutionContext.Run(context, act => ((Action)act!).Invoke(), nextAction);
                    }
                }
            })
            { IsBackground = true }.Start();
        }
    }



    public static void QueueUserWorkItem(Action action)
    {
        _actionQueue.Add((action, ExecutionContext.Capture()));
    }
}


public static class ContextedCustomThreadPoolSamples
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
        _threadCount = 55;
        AsyncLocal<int> mod = new();
        for (int i = 0; i < _threadCount; ++i)
        {
            mod.Value = 10 * i;
            MyThreadPoolWithContext.QueueUserWorkItem(() =>
                InstanceMethod(1 + mod.Value, 5 + mod.Value, 10001 + mod.Value, 10005 + mod.Value));
        }

        _resetEvent.Wait();

        Console.WriteLine("All fin");
    }
}
