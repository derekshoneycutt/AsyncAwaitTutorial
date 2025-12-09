/*
 * =====================================================
 *         Step 10 : Make a chained asynchronous version
 * 
 *  Here, we want to finally make a fully asynchronous version of
 *  our method, using the old chained continuations method.
 *  This showed up in the early days of asynchrony and was largely
 *  replaced with async/await. It shows a lot of the motivations
 *  for the new pattern.
 *  
 *  
 *  A.  Copy Step 9. We will reuse all of this.
 *  
 *  B.  Create the CompletedTask static property to represent
 *      a task that has already been completed.
 *      
 *  C.  First, update the existing ContinueWith to return
 *      a task that tracks when the continuation method
 *      has completed.
 *      
 *  D.  Use the existing Run and ContinueWith to create
 *      new copies that take a Func<MyTask>. The continuation
 *      must wait for the internal MyTask to return.
 *      The new Run method will need a new RunAsyncTask structure.
 *      The next sample will show us how the standard TPL
 *      handles this somewhat differently, but it allows
 *      us to explore the chaining method with our
 *      task at this time.
 *      
 *  E.  Refactor the InstanceMethod into a chain of ContinueWith
 *      calls to make it fully asynchronous.
 *      We should return a MyTask from the method after this,
 *      so we can remove the Task.Run earlier in Run method.
 *      
 * Refactoring requires some thought about how the algorithm
 * we are constructing can be done with pseudo-recursion
 * instead of loops. This makes it somewhat difficult.
 * However, it is also really good practice to understand
 * what the compiler is doing with async/await.
 * 
 * =====================================================
*/

using System.Runtime.ExceptionServices;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates creating an asynchronous chain of work utilizing the custom tasks previously created
/// </summary>
public class MyTaskAsyncChainSample : ITutorialSample
{
    /// <summary>
    /// The custom task class to represent work being done in the thread pool
    /// </summary>
    public class MyTask
    {
        // We will use CompletedTask during this sample, so add an implementation of it here.

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
        /// State structure to send to the thread pool concerning an async task to run; includes the action and the tracking task structure
        /// </summary>
        private readonly record struct RunAsyncTask(
            Func<MyTask> Action,
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
        private void Complete(Exception? ex)
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
        /// <returns>A Task that completes when the continuation action has completed.</returns>
        public MyTask ContinueWith(Action action)
        {
            // Update to return a Task that completes once the continuation has completed
            MyTask task = new();

            lock (_synchronize)
            {
                SetContinuationUnprotected(() =>
                {
                    action();

                    task.SetResult();
                });
            }

            return task;
        }

        /// <summary>
        /// Add a continuation action to the task that executes once the initial task has completed.
        /// </summary>
        /// <param name="action">The action to perform once the initial task has completed.</param>
        /// <returns>A Task that completes once the continuation task has also completed.</returns>

        public MyTask ContinueWith(Func<MyTask> action)
        {
            // Add a version that handles an asynchronous continuation method that should be tracked
            MyTask task = new();

            lock (_synchronize)
            {
                SetContinuationUnprotected(() =>
                {
                    MyTask next = action();
                    next.ContinueWith(() =>
                    {
                        if (next._exception is not null)
                        {
                            task.SetException(next._exception);
                        }
                        else
                        {
                            task.SetResult();
                        }
                    });
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
        /// Runs the specified action as a task on the thread pool.
        /// </summary>
        /// <param name="action">The action to run on the thread pool.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static MyTask Run(Func<MyTask> action)
        {
            // Add a version that handles an asynchronous method that should be tracked
            MyTask task = new();

            ThreadPool.QueueUserWorkItem<RunAsyncTask>(task =>
            {
                try
                {
                    MyTask next = task.Action();
                    next.ContinueWith(() =>
                    {
                        if (next._exception is not null)
                        {
                            task.Task.SetException(next._exception);
                        }
                        else
                        {
                            task.Task.SetResult();
                        }
                    });
                }
                catch (Exception ex)
                {
                    task.Task.SetException(ex);
                }
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
    /// The instance method to run as tasks.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    public static MyTask InstanceMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        // Completely refactor to use chains of ContinueWith so that it is fully asynchronous for the first time
        (int value, int currentEnd) = firstStart <= firstEnd ? (firstStart, firstEnd) : (firstEnd, firstStart);
        MyTask IncrementAndPrint(int end)
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
            ++value;

            if (value <= end)
            {
                return MyTask.Delay(1000)
                    .ContinueWith(() => IncrementAndPrint(end));
            }
            return MyTask.CompletedTask;
        }

        Console.WriteLine($"Writing values {identifier} / {Environment.CurrentManagedThreadId}");

        return MyTask.Delay(1000)
            .ContinueWith(() => IncrementAndPrint(currentEnd))
            .ContinueWith(() =>
            {
                (value, currentEnd) = secondStart <= secondEnd ? (secondStart, secondEnd) : (firstEnd, secondStart);
                value = secondStart;
                return MyTask.Delay(1000);
            })
            .ContinueWith(() => IncrementAndPrint(currentEnd))
            .ContinueWith(() => Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}"));
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(CancellationToken cancellationToken)
    {
        int actionCount = 55;
        List<MyTask> tasks = [];
        AsyncLocal<int> mod = new();
        for (int i = 0; i < actionCount; ++i)
        {
            mod.Value = 10 * i;
            string identifier = $"Action {i}";
            // We don't need to do Task.Run any more because the method returns a direct Task already
            tasks.Add(
                InstanceMethod(identifier,
                    1 + mod.Value, 5 + mod.Value,
                    1001 + mod.Value, 1005 + mod.Value));
        }

        MyTask.WhenAll(tasks).Wait();

        Console.WriteLine("All fin");
    }
}
