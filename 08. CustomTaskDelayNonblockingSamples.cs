using System.CommandLine;
using System.Runtime.ExceptionServices;

namespace AsyncAwaitTutorial;


public class MyTaskWithDelayNonBlocking
{
    public static MyTaskWithDelayNonBlocking CompletedTask
    {
        get
        {
            MyTaskWithDelayNonBlocking ret = new();
            ret.SetResult();
            return ret;
        }
    }


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


    public MyTaskWithDelayNonBlocking ContinueWith(Action action)
    {
        MyTaskWithDelayNonBlocking returnTask = new();

        void Callback()
        {
            action();

            returnTask.SetResult();
        }

        _synchronize.Wait();
        try
        {
            SetContinuationUnprotected(Callback);
        }
        finally
        {
            _synchronize.Release();
        }

        return returnTask;
    }


    public MyTaskWithDelayNonBlocking ContinueWith(Func<MyTaskWithDelayNonBlocking> action)
    {
        MyTaskWithDelayNonBlocking returnTask = new();

        void Callback()
        {
            MyTaskWithDelayNonBlocking followTask = action();
            followTask.ContinueWith(() =>
            {
                if (followTask._exception is not null)
                {
                    returnTask.SetException(followTask._exception);
                }
                else
                {
                    returnTask.SetResult();
                }
            });
        }

        _synchronize.Wait();
        try
        {
            SetContinuationUnprotected(Callback);
        }
        finally
        {
            _synchronize.Release();
        }

        return returnTask;
    }


    public static MyTaskWithDelayNonBlocking Run(Action action)
    {
        MyTaskWithDelayNonBlocking returnTask = new();

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


    public static MyTaskWithDelayNonBlocking Run(Func<MyTaskWithDelayNonBlocking> action)
    {
        MyTaskWithDelayNonBlocking returnTask = new();

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                MyTaskWithDelayNonBlocking followTask = action();
                followTask.ContinueWith(() =>
                {
                    if (followTask._exception is not null)
                    {
                        returnTask.SetException(followTask._exception);
                    }
                    else
                    {
                        returnTask.SetResult();
                    }
                });
            }
            catch (Exception ex)
            {
                returnTask.SetException(ex);
                return;
            }
        });

        return returnTask;
    }



    public static MyTaskWithDelayNonBlocking WhenAll(params IEnumerable<MyTaskWithDelayNonBlocking> tasks)
    {
        MyTaskWithDelayNonBlocking returnTask = new();

        List<MyTaskWithDelayNonBlocking> useTasks = [.. tasks];
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

            foreach (MyTaskWithDelayNonBlocking task in useTasks)
            {
                task.ContinueWith(Continuation);
            }
        }

        return returnTask;
    }



    public static MyTaskWithDelayNonBlocking Delay(int timeout)
    {
        MyTaskWithDelayNonBlocking task = new();
        new Timer(_ => task.SetResult()).Change(timeout, -1);
        return task;
    }
}

public static class CustomTaskDelayNonBlockingSamples
{
    public static MyTaskWithDelayNonBlocking InstanceMethod(
        int firstStart, int firstMax, int secondStart, int secondMax)
    {
        int i = firstStart;
        MyTaskWithDelayNonBlocking IncrementAndPrint(int max)
        {
            Console.WriteLine(i);
            ++i;

            if (i <= max)
            {
                return MyTaskWithDelayNonBlocking.Delay(1000)
                    .ContinueWith(() => IncrementAndPrint(max));
            }


            return MyTaskWithDelayNonBlocking.CompletedTask;
        }

        return MyTaskWithDelayNonBlocking.Run(() =>
        {
            Console.WriteLine($"Writing values: {Environment.CurrentManagedThreadId}");

            return MyTaskWithDelayNonBlocking.Delay(1000)
                .ContinueWith(() => IncrementAndPrint(firstMax)
                    .ContinueWith(() => MyTaskWithDelayNonBlocking.Delay(1000)
                        .ContinueWith(() =>
                        {
                            i = secondStart;
                            return IncrementAndPrint(secondMax)
                                .ContinueWith(() => Console.WriteLine("Fin"));
                        })));
        });
    }


    public static void Run(ParseResult parseResult)
    {
        int threadCount = 55;
        AsyncLocal<int> mod = new();
        List<MyTaskWithDelayNonBlocking> tasks = [];
        for (int i = 0; i < threadCount; ++i)
        {
            mod.Value = 10 * i;
            tasks.Add(
                InstanceMethod(1 + mod.Value, 5 + mod.Value, 10001 + mod.Value, 10005 + mod.Value));
        }

        MyTaskWithDelayNonBlocking.WhenAll(tasks).Wait();
    }
}
