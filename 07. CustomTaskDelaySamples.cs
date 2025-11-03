using System.CommandLine;
using System.Runtime.ExceptionServices;

namespace AsyncAwaitTutorial;


public class MyTaskWithDelay
{
    private readonly SemaphoreSlim _synchronize = new(1);

    private bool _completed = false;

    private Exception? _exception = null;

    private Action? _continuation = null;

    private ExecutionContext? _executionContext = null;

    public bool IsCompleted
    {
        get
        {
            _synchronize.Wait();
            try
            {
                return _completed;
            }
            finally
            {
                _synchronize.Release();
            }
        }
    }

    private void Complete(Exception? ex)
    {
        _synchronize.Wait();
        try
        {
            if (_completed)
            {
                throw new InvalidOperationException("Cannot complete an already completed task.");
            }

            _completed = true;
            _exception = ex;

            if (_continuation is not null)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    if (_executionContext is null)
                    {
                        _continuation();
                    }
                    else
                    {
                        ExecutionContext.Run(_executionContext, act => ((Action)act!).Invoke(), _continuation);
                    }
                });
            }
        }
        finally
        {
            _synchronize.Release();
        }
    }

    public void SetResult()
    {
        Complete(null);
    }

    public void SetException(Exception ex)
    {
        Complete(ex);
    }


    private void SetContinuationUnprotected(Action action)
    {
        if (_completed)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (_executionContext is null)
                {
                    action();
                }
                else
                {
                    ExecutionContext.Run(_executionContext, act => ((Action)act!).Invoke(), action);
                }
            });
        }
        else
        {
            _continuation = action;
            _executionContext = ExecutionContext.Capture();
        }
    }


    public void Wait()
    {
        ManualResetEventSlim? reset = null;

        _synchronize.Wait();
        try
        {
            if (!_completed)
            {
                reset = new();
                SetContinuationUnprotected(reset.Set);
            }
        }
        finally
        {
            _synchronize.Release();
        }

        reset?.Wait();

        if (_exception is not null)
        {
            ExceptionDispatchInfo.Throw(_exception);
        }
    }



    public void ContinueWith(Action action)
    {
        _synchronize.Wait();
        try
        {
            SetContinuationUnprotected(action);
        }
        finally
        {
            _synchronize.Release();
        }
    }

    public static MyTaskWithDelay Run(Action action)
    {
        MyTaskWithDelay returnTask = new();

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                returnTask.SetException(ex);
                return;
            }

            returnTask.SetResult();
        });

        return returnTask;
    }



    public static MyTaskWithDelay WhenAll(params IEnumerable<MyTaskWithDelay> tasks)
    {
        MyTaskWithDelay returnTask = new();

        List<MyTaskWithDelay> useTasks = [.. tasks];
        if (useTasks.Count < 1)
        {
            returnTask.SetResult();
        }
        else
        {
            int remaining = useTasks.Count;

            void Continuation()
            {
                if (Interlocked.Decrement(ref remaining) < 1)
                {
                    returnTask.SetResult();
                }
            }

            foreach (MyTaskWithDelay task in useTasks)
            {
                task.ContinueWith(Continuation);
            }
        }

        return returnTask;
    }



    public static MyTaskWithDelay Delay(int timeout)
    {
        MyTaskWithDelay task = new();
        new Timer(_ => task.SetResult()).Change(timeout, -1);
        return task;
    }
}

public static class CustomTaskDelaySamples
{
    public static void InstanceMethod(
        int firstStart, int firstMax, int secondStart, int secondMax)
    {
        Console.WriteLine($"Writing values: {Environment.CurrentManagedThreadId}");

        for (int i = firstStart; i <= firstMax; i++)
        {
            MyTaskWithDelay.Delay(1000).Wait();
            Console.WriteLine(i);
        }
        for (int i = secondStart; i <= secondMax; i++)
        {
            MyTaskWithDelay.Delay(1000).Wait();
            Console.WriteLine(i);
        }

        Console.WriteLine("Fin");
    }


    public static void Run(ParseResult parseResult)
    {
        int threadCount = 55;
        AsyncLocal<int> mod = new();
        List<MyTaskWithDelay> tasks = [];
        for (int i = 0; i < threadCount; ++i)
        {
            mod.Value = 10 * i;
            tasks.Add(MyTaskWithDelay.Run(() =>
                InstanceMethod(1 + mod.Value, 5 + mod.Value, 10001 + mod.Value, 10005 + mod.Value)));
        }

        MyTaskWithDelay.WhenAll(tasks).Wait();
    }
}
