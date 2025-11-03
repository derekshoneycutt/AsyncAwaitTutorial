using System;
using System.CommandLine;
using System.Runtime.ExceptionServices;

namespace AsyncAwaitTutorial;


public class MyTask
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


    public static MyTask Run(Action action)
    {
        MyTask returnTask = new();

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
}

public static class CustomTaskSamples
{
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
    }


    public static void Run(ParseResult parseResult)
    {
        int threadCount = 55;
        AsyncLocal<int> mod = new();
        List<MyTask> tasks = [];
        for (int i = 0; i < threadCount; ++i)
        {
            mod.Value = 10 * i;
            tasks.Add(MyTask.Run(() =>
                InstanceMethod(1 + mod.Value, 5 + mod.Value, 10001 + mod.Value, 10005 + mod.Value)));
        }

        foreach (MyTask task in tasks)
        {
            task.Wait();
        }
    }
}
