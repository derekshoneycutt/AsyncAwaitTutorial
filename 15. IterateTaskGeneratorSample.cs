/*
 * =====================================================
 *         Step 15 : Hacking iterators for async code
 * 
 *  async/await and IEnumerable iterators share vast
 *  amounts of code at the compiler level, so we will
 *  utilize the iterators with yield return and our own
 *  MyTask from previous samples to simulate async/await
 *  type programming for the first time.
 *  
 *  
 *  A.  Copy Step 10. We will update this code.
 *      Comment or remove the InstanceMethod from there
 *      and instead copy over the InstanceMethod from
 *      step 9. We can use this more effectively.
 *  
 *  B.  Update the InstanceMethod to return IEnumerable<MyTask>
 *      and change the .Wait() calls to instead yield return
 *      the MyTask object.
 *      
 *  C.  Create the Iterate method and modify Run to use it
 *      and the new InstanceMethod.
 *      
 *      
 * This is a pretty simple step after everything we
 * have done to get here, but we now have something almost
 * identical to async/await with our own custom task!
 * 
 * =====================================================
*/

using System.Runtime.ExceptionServices;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates using IEnumerable iterator to simulate async/await style
/// </summary>
/// <remarks>
/// For this sample, we go back to step 9--Using Custom Task.Delay--and try out what happens
/// if we pretend yield return is kind of like await in async/await. In reality, the code compiled between these two is remarkably similar.
/// We have to add and use an Iterate method as well as refactoring it to use the yield return method.
/// This isn't a great thing for production code, but it demonstrates a transitional phase to understand what is happening.
/// </remarks>
public class IterateTaskGeneratorSample : ITutorialSample
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
        /// <exception cref="System.InvalidOperationException">Cannot complete an already completed task.</exception>
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
        public MyTask ContinueWith(Action action)
        {
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
    /// Helper method that will iterate over a collection of tasks and run them subsequently, as an asynchronous operation.
    /// </summary>
    /// <param name="tasks">The tasks to iterate over.</param>
    /// <returns>A Task that represents the asynchronous operation</returns>
    public static MyTask Iterate(IEnumerable<MyTask> tasks)
    {
        MyTask task = new();

        IEnumerator<MyTask> enumerator = tasks.GetEnumerator();

        void MoveNext()
        {
            try
            {
                if (enumerator.MoveNext())
                {
                    enumerator.Current.ContinueWith(MoveNext);
                    return;
                }
            }
            catch (Exception ex)
            {
                task.SetException(ex);
                return;
            }

            task.SetResult();
        }

        MoveNext();

        return task;
    }

    /// <summary>
    /// Returns an iterator that loops over 2 ranges of integers subsequently.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first range start.</param>
    /// <param name="firstEnd">The first range maximum.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondEnd">The second range maximum.</param>
    /// <returns>An <see cref="IEnumerable{Int32}"/> that loops over 2 integer ranges subsequently.</returns>
    public static IEnumerable<MyTask> InstanceMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        // We return IEnumerable<MyTask> instead of void and do a yield return on the MyTask.Delay instead of Wait.
        (int start, int end) = firstStart <= firstEnd ? (firstStart, firstEnd) : (firstEnd, firstStart);
        for (int value = start; value <= end; ++value)
        {
            yield return MyTask.Delay(1000);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }
        (start, end) = secondStart <= secondEnd ? (secondStart, secondEnd) : (secondEnd, secondStart);
        for (int value = start; value <= end; ++value)
        {
            yield return MyTask.Delay(1000);
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
        int actionCount = 55;
        List<MyTask> tasks = [];
        AsyncLocal<int> mod = new();
        for (int i = 0; i < actionCount; ++i)
        {
            mod.Value = 10 * i;
            string identifier = $"Action {i}";
            // Now we call Iterate on the IEnumerable InstanceMethod returns; unfortunately back to an intermediary and not directly adding return from InstanceMethod
            tasks.Add(Iterate(
                InstanceMethod(identifier,
                    1 + mod.Value, 5 + mod.Value,
                    1001 + mod.Value, 1005 + mod.Value)));
        }

        MyTask.WhenAll(tasks).Wait();

        Console.WriteLine("All fin");
    }
}
