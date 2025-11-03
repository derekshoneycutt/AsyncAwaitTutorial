using System.Runtime.ExceptionServices;

namespace AsyncAwaitTutorial;



/// <summary>
/// This sample demonstrates creating a custom implementation of Task.WhenAll with the prevoius custom tasks.
/// </summary>
public static class MyTaskWhenAllSamples
{

    /// <summary>
    /// The custom task class to represent work being done in the thead pool
    /// </summary>
    public class MyTask
    {
        /// <summary>
        /// The semaphore used to synchronize between several threads
        /// </summary>
        private readonly SemaphoreSlim _synchronize = new(1);

        /// <summary>
        /// Flag indicating whether this task has completed yet
        /// </summary>
        private bool _completed = false;

        /// <summary>
        /// The exception that has occurred during the work, or <c>null</c> if no exception has occurred
        /// </summary>
        private Exception? _exception = null;

        /// <summary>
        /// The action to continue with once the task has completed, or <c>null</c> if no continuation has been added to this task
        /// </summary>
        private Action? _continuation = null;

        /// <summary>
        /// The execution context that the task information should be run under
        /// </summary>
        private ExecutionContext? _executionContext = null;

        /// <summary>
        /// Gets a value indicating whether this task has completed operations.
        /// </summary>
        /// <value>
        /// Is <c>true</c> if this task has completed operatoins; otherwise, <c>false</c>.
        /// </value>
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

        /// <summary>
        /// Marks the task as complete, with or without an exception
        /// </summary>
        /// <param name="ex">The exception that should close the task, or <c>null</c> if no exception occurred.</param>
        /// <exception cref="System.InvalidOperationException">Cannot complete an already completed task.</exception>
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

        /// <summary>
        /// Set the task as completed.
        /// </summary>
        public void SetResult()
        {
            Complete(null);
        }

        /// <summary>
        /// Set the task as completed due to a given exception.
        /// </summary>
        public void SetException(Exception ex)
        {
            Complete(ex);
        }


        /// <summary>
        /// Sets the continuation for the task without any semaphore protection.
        /// </summary>
        /// <remarks>
        /// Only use this with another method that blocks on the semaphore already
        /// </remarks>
        /// <param name="action">The action to queue into the thread pool.</param>
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

        /// <summary>
        /// Block and wait for the task to complete.
        /// </summary>
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


        /// <summary>
        /// Add a continuation action to the task that executes once the initial task has completed.
        /// </summary>
        /// <param name="action">The action to perform once the initial task has completed.</param>
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


        /// <summary>
        /// Runs the specified action as a task on the threadpool.
        /// </summary>
        /// <param name="action">The action to run on the threadpool.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
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


        /// <summary>
        /// Wait until all of the provided tasks have completed, as an asynchronos operation
        /// </summary>
        /// <param name="tasks">The tasks to wait for the ocmpletion of</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static MyTask WhenAll(params IEnumerable<MyTask> tasks)
        {
            MyTask returnTask = new();

            List<MyTask> useTasks = [.. tasks];
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

                foreach (MyTask task in useTasks)
                {
                    task.ContinueWith(Continuation);
                }
            }

            return returnTask;
        }
    }



    /// <summary>
    /// The instance method to run as tasks.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstMax">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondMax">The second maximum value, completing the second range.</param>
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


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    public static void Run()
    {
        int threadCount = 55;
        List<MyTask> tasks = [];
        for (int i = 0; i < threadCount; ++i)
        {
            int mod = 10 * i;
            string action = $"Action {i}";
            tasks.Add(MyTask.Run(() =>
                InstanceMethod(action, 1 + mod, 5 + mod, 10001 + mod, 10005 + mod)));
        }

        MyTask.WhenAll(tasks).Wait();
    }
}
