using System.CommandLine;
using System.Runtime.ExceptionServices;

namespace AsyncAwaitTutorial;


public class IteratingTask
{
    public static IteratingTask CompletedTask
    {
        get
        {
            IteratingTask ret = new();
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


    public IteratingTask ContinueWith(Action action)
    {
        IteratingTask returnTask = new();

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


    public IteratingTask ContinueWith(Func<IteratingTask> action)
    {
        IteratingTask returnTask = new();

        void Callback()
        {
            IteratingTask followTask = action();
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


    public static IteratingTask Run(Action action)
    {
        IteratingTask returnTask = new();

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


    public static IteratingTask Run(Func<IteratingTask> action)
    {
        IteratingTask returnTask = new();

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                IteratingTask followTask = action();
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



    public static IteratingTask WhenAll(params IEnumerable<IteratingTask> tasks)
    {
        IteratingTask returnTask = new();

        List<IteratingTask> useTasks = [.. tasks];
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

            foreach (IteratingTask task in useTasks)
            {
                task.ContinueWith(Continuation);
            }
        }

        return returnTask;
    }



    public static IteratingTask Delay(int timeout)
    {
        IteratingTask task = new();
        new Timer(_ => task.SetResult()).Change(timeout, -1);
        return task;
    }




    public static IteratingTask Iterate(IEnumerable<IteratingTask> tasks)
    {
        IteratingTask returnTask = new();

        IEnumerator<IteratingTask> enumerator = tasks.GetEnumerator();

        void MoveNext()
        {
            try
            {
                if (enumerator.MoveNext())
                {
                    IteratingTask task = enumerator.Current;
                    task.ContinueWith(MoveNext);
                    return;
                }
            }
            catch (Exception ex)
            {
                returnTask.SetException(ex);
                return;
            }


            returnTask.SetResult();
        }

        MoveNext();

        return returnTask;
    }


}



public static class IterateTaskGeneratorSample
{

    public static IEnumerable<IteratingTask> DoubleLoop(
        int firstStart, int firstMax, int secondStart, int secondMax)
    {
        Console.WriteLine($"Writing values: {Environment.CurrentManagedThreadId}");

        for (int value = firstStart; value <= firstMax; ++value)
        {
            yield return IteratingTask.Delay(1000);
            Console.WriteLine(value);
        }
        for (int value = secondStart; value <= secondMax; ++value)
        {
            yield return IteratingTask.Delay(1000);
            Console.WriteLine(value);
        }

        Console.WriteLine("Fin");
    }


    public static void Run(ParseResult parseResult)
    {
        int threadCount = 55;
        AsyncLocal<int> mod = new();
        List<IteratingTask> tasks = [];
        for (int i = 0; i < threadCount; ++i)
        {
            mod.Value = 10 * i;
            tasks.Add(IteratingTask.Iterate(
                DoubleLoop(1 + mod.Value, 5 + mod.Value, 10001 + mod.Value, 10005 + mod.Value)));
        }

        IteratingTask.WhenAll(tasks).Wait();
    }
}
