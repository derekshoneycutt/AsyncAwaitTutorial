using System.CommandLine;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace AsyncAwaitTutorial;


public class AwaitableTask
{
    public static AwaitableTask CompletedTask
    {
        get
        {
            AwaitableTask ret = new();
            ret.SetResult();
            return ret;
        }
    }


    private readonly SemaphoreSlim _synchronize = new(1);

    private bool _completed = false;

    private Exception? _exception = null;

    private Action? _continuation = null;

    private ExecutionContext? _executionContext = null;


    public struct Awaiter(AwaitableTask task) : INotifyCompletion
    {
        public readonly Awaiter GetAwaiter() => this;

        public readonly bool IsCompleted => task.IsCompleted;

        public readonly void OnCompleted(Action continuation)
        {
            task.ContinueWith(continuation);
        }

        public readonly void GetResult() => task.Wait();
    }


    public Awaiter GetAwaiter() => new(this);

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


    public AwaitableTask ContinueWith(Action action)
    {
        AwaitableTask returnTask = new();

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


    public AwaitableTask ContinueWith(Func<AwaitableTask> action)
    {
        AwaitableTask returnTask = new();

        void Callback()
        {
            AwaitableTask followTask = action();
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


    public static AwaitableTask Run(Action action)
    {
        AwaitableTask returnTask = new();

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


    public static AwaitableTask Run(Func<AwaitableTask> action)
    {
        AwaitableTask returnTask = new();

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                AwaitableTask followTask = action();
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



    public static AwaitableTask WhenAll(params IEnumerable<AwaitableTask> tasks)
    {
        AwaitableTask returnTask = new();

        List<AwaitableTask> useTasks = [.. tasks];
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

            foreach (AwaitableTask task in useTasks)
            {
                task.ContinueWith(Continuation);
            }
        }

        return returnTask;
    }



    public static AwaitableTask Delay(int timeout)
    {
        AwaitableTask task = new();
        new Timer(_ => task.SetResult()).Change(timeout, -1);
        return task;
    }




    public static AwaitableTask Iterate(IEnumerable<AwaitableTask> tasks)
    {
        AwaitableTask returnTask = new();

        IEnumerator<AwaitableTask> enumerator = tasks.GetEnumerator();

        void MoveNext()
        {
            try
            {
                if (enumerator.MoveNext())
                {
                    AwaitableTask task = enumerator.Current;
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



public static class AwaitableCustomSample
{

    public static async Task DoubleLoop(
        int firstStart, int firstMax, int secondStart, int secondMax)
    {
        Console.WriteLine($"Writing values: {Environment.CurrentManagedThreadId}");

        for (int value = firstStart; value <= firstMax; ++value)
        {
            await AwaitableTask.Delay(1000);
            Console.WriteLine($"{Environment.CurrentManagedThreadId} => {value}");
        }
        for (int value = secondStart; value <= secondMax; ++value)
        {
            await AwaitableTask.Delay(1000);
            Console.WriteLine($"{Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine("Fin");
    }


    public static async Task Run(ParseResult parseResult, CancellationToken cancellationToken)
    {
        int threadCount = 55;
        AsyncLocal<int> mod = new();
        List<Task> tasks = [];
        for (int i = 0; i < threadCount; ++i)
        {
            mod.Value = 10 * i;
            tasks.Add(
                DoubleLoop(1 + mod.Value, 5 + mod.Value, 10001 + mod.Value, 10005 + mod.Value));
        }

        await Task.WhenAll(tasks);
    }
}
