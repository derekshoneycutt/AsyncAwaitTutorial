using System.Runtime.ExceptionServices;

namespace AsyncAwaitTutorial;



/// <summary>
/// This sample demonstrates creating an asynchronous chain of work utilizing the custom tasks previously created
/// </summary>
public static class MyTaskAsyncChainSamples
{

    /// <summary>
    /// The custom task class to represent work being done in the thread pool
    /// </summary>
    public class MyTask
    {
        /// <summary>
        /// Gets a completed task
        /// </summary>
        public static MyTask CompletedTask
        {
            get
            {
                MyTask ret = new();
                ret.SetResult();
                return ret;
            }
        }


        /// <summary>
        /// The lock object used to synchronize between several threads
        /// </summary>
        private readonly Lock _synchronize = new();

        /// <summary>
        /// Flag indicating if the task has been completed or not.
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
        /// Is <c>true</c> if this task has completed operations; otherwise, <c>false</c>.
        /// </value>
        public bool IsCompleted
        {
            get
            {
                lock (_synchronize)
                {
                    return _completed;
                }
            }
        }

        /// <summary>
        /// Executes the specified action on the specified context, if the context is given.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="executionContext">The execution context to execute on.</param>
        private static void Execute(
            Action action,
            ExecutionContext? executionContext = null)
        {
            if (executionContext is null)
            {
                action();
            }
            else
            {
                ExecutionContext.Run(executionContext, act => ((Action)act!).Invoke(), action);
            }
        }

        /// <summary>
        /// Marks the task as complete, with or without an exception
        /// </summary>
        /// <param name="ex">The exception that should close the task, or <c>null</c> if no exception occurred.</param>
        /// <exception cref="System.InvalidOperationException">Cannot complete an already completed task.</exception>
        private void Complete(Exception? ex)
        {
            lock (_synchronize)
            {
                if (IsCompleted)
                {
                    throw new InvalidOperationException("Cannot complete an already completed task.");
                }

                _completed = true;
                _exception = ex;

                if (_continuation is not null)
                {
                    ThreadPool.QueueUserWorkItem(_ => Execute(_continuation, _executionContext));
                }
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
            if (IsCompleted)
            {
                ThreadPool.QueueUserWorkItem(_ => Execute(action, _executionContext));
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

            lock (_synchronize)
            {
                if (!IsCompleted)
                {
                    reset = new();
                    SetContinuationUnprotected(reset.Set);
                }
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
        public MyTask ContinueWith(Action action)
        {
            MyTask returnTask = new();

            lock (_synchronize)
            {
                SetContinuationUnprotected(() =>
                {
                    action();

                    returnTask.SetResult();
                });
            }

            return returnTask;
        }



        /// <summary>
        /// Add a continuation action to the task that executes once the initial task has completed.
        /// </summary>
        /// <param name="action">The action to perform once the initial task has completed.</param>
        /// <returns>A Task that completes once the continuation task has also completed.</returns>

        public MyTask ContinueWith(Func<MyTask> action)
        {
            MyTask returnTask = new();

            lock (_synchronize)
            {
                SetContinuationUnprotected(() =>
                {
                    MyTask followTask = action();
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
                });
            }

            return returnTask;
        }



        /// <summary>
        /// Runs the specified action as a task on the thread pool.
        /// </summary>
        /// <param name="action">The action to run on the thread pool.</param>
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
        /// Runs the specified action as a task on the thread pool.
        /// </summary>
        /// <param name="action">The action to run on the thread pool.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static MyTask Run(Func<MyTask> action)
        {
            MyTask returnTask = new();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    MyTask followTask = action();
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




        /// <summary>
        /// Wait until all of the provided tasks have completed, as an asynchronous operation
        /// </summary>
        /// <param name="tasks">The tasks to wait for the completion of</param>
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


        /// <summary>
        /// Delays for a specified timeout period as an asynchronous operation.
        /// </summary>
        /// <param name="timeout">The timeout period to delay for.</param>
        /// <returns>A Task that represents the asynchronous operation, completing at the end of hte given timeout.</returns>
        public static MyTask Delay(int timeout)
        {
            MyTask task = new();
            new Timer(_ => task.SetResult()).Change(timeout, -1);
            return task;
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
    public static MyTask InstanceMethod(
        string identifier,
        int firstStart, int firstMax, int secondStart, int secondMax)
    {
        int i = firstStart;
        MyTask IncrementAndPrint(int max)
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
            ++i;

            if (i <= max)
            {
                return MyTask.Delay(1000)
                    .ContinueWith(() => IncrementAndPrint(max));
            }


            return MyTask.CompletedTask;
        }

        return MyTask.Run(() =>
        {
            Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

            return MyTask.Delay(1000)
                .ContinueWith(() => IncrementAndPrint(firstMax)
                    .ContinueWith(() => MyTask.Delay(1000)
                        .ContinueWith(() =>
                        {
                            i = secondStart;
                            return IncrementAndPrint(secondMax)
                                .ContinueWith(() => Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}"));
                        })));
        });
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
            tasks.Add(InstanceMethod(
                action, 1 + mod, 5 + mod, 10001 + mod, 10005 + mod));
        }

        MyTask.WhenAll(tasks).Wait();
    }
}
