using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates using a custom Awaiter to introduce async/await
/// </summary>
public class AwaitableCustomSample : ITutorialSample
{
    /// <summary>
    /// The custom task class to represent work being done in the thread pool
    /// </summary>
    public class MyTask
    {
        /// <summary>
        /// Structure to store the continuation information currently requested for the task
        /// </summary>
        private readonly record struct RunContinuation(
            Action? Continuation,
            ExecutionContext? ExecutionContext);

        /// <summary>
        /// State structure to send to the thread pool concerning a task to run; includes the action and the tracking task structure
        /// </summary>
        private readonly record struct RunTask(
            Action Action,
            MyTask Task);

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
        private RunContinuation _continuation = new(null, null);

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
        /// <param name="continuation">The continuation data containing the action and the execution context to execute</param>
        private static void Execute(RunContinuation continuation)
        {
            if (continuation.Continuation is null)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem<RunContinuation>(continuation =>
            {
                if (continuation.ExecutionContext is null)
                {
                    continuation.Continuation!();
                }
                else
                {
                    ExecutionContext.Run(continuation.ExecutionContext, act => ((Action)act!).Invoke(), continuation.Continuation);
                }
            }, continuation, true);
        }

        /// <summary>
        /// Marks the task as complete, with or without an exception
        /// </summary>
        /// <param name="ex">The exception that should close the task, or <c>null</c> if no exception occurred.</param>
        /// <exception cref="System.InvalidOperationException">Cannot complete an already completed task.</exception>
        protected void Complete(Exception? ex)
        {
            lock (_synchronize)
            {
                if (_completed)
                {
                    throw new InvalidOperationException("Cannot complete an already completed task.");
                }

                _completed = true;
                _exception = ex;

                Execute(_continuation);
            }
        }

        /// <summary>
        /// Set the task as completed.
        /// </summary>
        public virtual void SetResult()
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
            RunContinuation continuation = new(action, ExecutionContext.Capture());
            if (_completed)
            {
                Execute(continuation);
            }
            else
            {
                _continuation = continuation;
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
                if (!_completed)
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
            MyTask task = new();

            lock (_synchronize)
            {
                SetContinuationUnprotected(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        task.SetException(ex);
                        return;
                    }

                    task.SetResult();
                });
            }

            return task;
        }

        /// <summary>
        /// Runs the specified action as a task on the thread pool.
        /// </summary>
        /// <param name="action">The action to run on the thread pool.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static MyTask Run(Action action)
        {
            MyTask task = new();

            ThreadPool.QueueUserWorkItem<RunTask>(task =>
            {
                try
                {
                    task.Action();
                }
                catch (Exception ex)
                {
                    task.Task.SetException(ex);
                    return;
                }

                task.Task.SetResult();
            }, new(action, task), true);

            return task;
        }

        /// <summary>
        /// Wait until all of the provided tasks have completed, as an asynchronous operation
        /// </summary>
        /// <param name="tasks">The tasks to wait for the completion of</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static MyTask WhenAll(params IEnumerable<MyTask> tasks)
        {
            MyTask task = new();

            List<MyTask> useTasks = [.. tasks];
            if (useTasks.Count < 1)
            {
                task.SetResult();
            }
            else
            {
                int remaining = useTasks.Count;

                void Continuation()
                {
                    if (Interlocked.Decrement(ref remaining) < 1)
                    {
                        task.SetResult();
                    }
                }

                foreach (MyTask useTask in useTasks)
                {
                    useTask.ContinueWith(Continuation);
                }
            }

            return task;
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
    /// Task structure used to store some result value of the task
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public class MyTask<TResult>
        : MyTask
    {
        /// <summary>
        /// The result value; default if not completed
        /// </summary>
        private TResult _result = default!;

        /// <summary>
        /// Gets the result. Waits for the task to complete if it is not completed already.
        /// </summary>
        public TResult Result
        {
            get
            {
                Wait();
                return _result;
            }
        }

        // We add the Awaiter struct and the GetAwaiter() method. This is a minimal implementation, but it is all that is required.

        /// <summary>
        /// The awaiter used to await on this task type in async/await
        /// </summary>
        /// <seealso cref="INotifyCompletion" />
        public struct Awaiter(MyTask<TResult> task) : INotifyCompletion
        {
            /// <summary>
            /// Gets a value indicating whether the task is completed.
            /// </summary>
            public readonly bool IsCompleted => task.IsCompleted;

            /// <summary>
            /// Gets the awaiter. Always just this.
            /// </summary>
            public readonly Awaiter GetAwaiter() => this;

            /// <summary>
            /// Gets the result. This task has no return, so just calls Wait();
            /// </summary>
            public readonly TResult GetResult() => task.Result;

            /// <summary>
            /// Called when the task is completed.
            /// </summary>
            /// <param name="continuation">The continuation to run after completion.</param>
            public readonly void OnCompleted(Action continuation)
            {
                task.ContinueWith(continuation);
            }
        }

        /// <summary>
        /// Gets the awaiter to use with async/await.
        /// </summary>
        /// <returns>A new <see cref="Awaiter"/> to use in await</returns>
        public Awaiter GetAwaiter() => new(this);

        /// <summary>
        /// Set the task as completed. This always throws.
        /// </summary>
        public override void SetResult()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Sets the task as completed with a specified result.
        /// </summary>
        /// <param name="value">The result value to specify.</param>
        public void SetResult(TResult value)
        {
            if (!IsCompleted)
            {
                _result = value;
                Complete(null);
            }
        }

        /// <summary>
        /// Add a continuation action to the task that executes once the initial task has completed.
        /// </summary>
        /// <param name="action">The action to perform once the initial task has completed.</param>
        public MyTask ContinueWith(Action<TResult> action)
        {
            return ContinueWith(() => action(_result));
        }
    }

    // We don't need an Iterate method any more

    /// <summary>
    /// Produces the specified ranges of values.
    /// </summary>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    /// <returns>A list of the produced values</returns>
    public static IEnumerable<MyTask<int>> Produce(
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        for (int value = firstStart; value <= firstEnd; ++value)
        {
            MyTask<int> returnTask = new();
            MyTask.Delay(1000).ContinueWith(() => returnTask.SetResult(value));
            yield return returnTask;
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            MyTask<int> returnTask = new();
            MyTask.Delay(1000).ContinueWith(() => returnTask.SetResult(value));
            yield return returnTask;
        }
    }

    /// <summary>
    /// Consumes the collection, printing each value to the screen
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The values to print to the screen.</param>
    public static async Task Consume(
        string identifier,
        IEnumerable<MyTask<int>> values)
    {
        // We update this to be async/await with our custom task type!
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        foreach (MyTask<int> valueTask in values)
        {
            int value = await valueTask;
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(CancellationToken cancellationToken)
    {
        // We only work with Tasks in this method now
        List<Task> tasks = [];
        AsyncLocal<int> mod = new();
        for (int index = 1; index <= 55; ++index)
        {
            mod.Value = 10 * index;
            string identifier = $"Action {index}";
            IEnumerable<MyTask<int>> values = Produce(
                1 + mod.Value, 5 + mod.Value,
                1001 + mod.Value, 1005 + mod.Value);
            // And we remove the wrapping call to Iterate, since we just get a full Task object now.
            tasks.Add(Consume(identifier, values));
        }

        // We can go ahead and await on the Task.WhenAll now, instead of Wait!
        await Task.WhenAll(tasks);

        Console.WriteLine("All fin");
    }
}
