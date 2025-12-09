/*
 * =====================================================
 *         Step 7 : Extend custom Task structure
 * 
 *  The major goal with this one is introduce a Task.Run
 *  equivalent and the concept of ContinueWith, initially
 *  by replacing the initial Wait implementation with one
 *  based on ContinueWith instead.
 *  
 *  
 *  A.  Copy Step 6. We will reuse all of this.
 *      
 *  B.  Rename the MyTaskCompletion to MyTask and add the
 *      Task.Run equivalent implementation, including creating
 *      the state structure to pass into the thread pool.
 *      
 *  C.  Update Run and InstanceMethod to use the new MyTask.Run method.
 *      This is mostly removing code that MyTask.Run will now cover
 *      for us.
 *      We can remove the temporary ThreadPoolState with this complete.
 *      
 *  D.  Create the ContinueWith implementation. This
 *      requires a new structure with 2 new properties:
 *      continuation and execution context.
 *      We take the Execute from the thread pool and use a similar
 *      method to execute the ContinueWith methods on the thread pool.
 *      
 *  E.  Refactor Wait to use the continuation and a local
 *      reset event instead.
 *      
 * This prepares us with the normal Task patterns we see throughout
 * async code in C#, and internally Wait gives us a good first example
 * of using the ContinueWith to continue async operations.
 * 
 * Of course, we are not trying to literally re-create the actual
 * Task class, so we take whatever liberties we want with the
 * style. This is just a demonstration, so we can have some fun
 * with records and stuff if we want.
 * 
 * =====================================================
*/

using System.Runtime.ExceptionServices;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates making a custom Task class using the standard ThreadPool class.
/// </summary>
/// <remarks>
/// The major goal with this one is introduce a Task.Run equivalent and the concept of ContinueWith,
/// initially by replacing the initial Wait implementation with one based on ContinueWith instead.
/// </remarks>
public class MyTaskSample : ITutorialSample
{
    // We don't need the old ThreadPoolState any more

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

        // Remove the ManualResetEvent for waiting and add an action for continuation and it's execution context to run on

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

        // Pull the Execute method from the custom ThreadPool implementation so we can have control with the Task to run actions on the appropriate execution context

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

                // Run the continuation on the thread pool, no more wait event to set *here*
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
            // Refactor the Wait method to use the continuation to set a reset event created here.

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
        public void ContinueWith(Action action)
        {
            lock (_synchronize)
            {
                SetContinuationUnprotected(action);
            }
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
    }

    /// <summary>
    /// The instance method to run as tasks.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    public static void InstanceMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        // Remove all the funny tracking we had to add before! We're back to just a normal looking method!
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        (int start, int end) = firstStart <= firstEnd ? (firstStart, firstEnd) : (firstEnd, firstStart);
        for (int value = start; value <= end; ++value)
        {
            Thread.Sleep(1000);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }
        (start, end) = secondStart <= secondEnd ? (secondStart, secondEnd) : (secondEnd, secondStart);
        for (int value = start; value <= end; ++value)
        {
            Thread.Sleep(1000);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");
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
            // Now use MyTask.Run to run the simpler method and track it the same!
            tasks.Add(MyTask.Run(() =>
                InstanceMethod(identifier,
                    1 + mod.Value, 5 + mod.Value,
                    1001 + mod.Value, 1005 + mod.Value)));
        }

        foreach (MyTask task in tasks)
        {
            task.Wait();
        }

        Console.WriteLine("All fin");
    }
}
