/*
 * =====================================================
 *         Step 6 : Create a basic Task Completion object
 * 
 *  We want to remove the static counter and ManualResetEvent
 *  from the previous samples, and we want to track individual
 *  work items on the thread queue. This introduces us to the
 *  concept of Task. Here, we just introduce the basic
 *  Task Completion pattern which we will see repeatedly
 *  and which serve as a basis for the reason for Task.
 *  Again, we are not trying to perfectly re-create Task
 *  or even the start of TaskCompletionSource, per se,
 *  but we do want to demonstrate the patterns and concepts.
 *  
 *  
 *  A.  Copy Step 5. We will reuse all of this.
 *      
 *  B.  Create a new MyTaskCompletion class that will
 *      serve to track the progress of a work item on the
 *      thread pool. We will need a synchronization Lock,
 *      a completed flag, and an exception for fields.
 *      IsCompleted will be our one public property.
 *     
 *  C.  We want methods SetResult, SetException, and Wait.
 *      We can have a Complete private method to work for
 *      both SetResult and SetException.
 *      
 *  D.  Introduce a list of these Task Completion objects in the
 *      Run method, and create and pass one to each instance of
 *      InstanceMethod. Wait on these Tasks at the end of Run.
 *      
 *  E.  Update InstanceMethod for both SetResult and SetException,
 *      requiring a wrapping try...catch block.
 *      
 * This is a lift to create a structure for tracking the Tasks
 * that we run on the ThreadPool. However, the pattern produced
 * in the InstanceMethod of this sample is repeated again and again
 * in the following samples, so it is important to understand it.
 * 
 * =====================================================
*/

using System.Runtime.ExceptionServices;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates creating a basic task completion class to track tasks on the thread pool.
/// </summary>
public class MyTaskCompletionSample : ITutorialSample
{
    /// <summary>
    /// State structure to send to the instance method for work items queued on the thread pool
    /// </summary>
    readonly record struct ThreadPoolState(string Identifier, AsyncLocal<int> Mod);

    /// <summary>
    /// Custom class representing when a task is completed on the thread pool
    /// </summary>
    public class MyTaskCompletion
    {
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
        /// The wait event that Wait method will wait on for the completion of the event.
        /// </summary>
        private readonly ManualResetEventSlim _waitEvent = new(false);


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

                _waitEvent.Set();
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
        /// Block and wait for the task to complete.
        /// Re-throws any exceptions that are reported to complete the task.
        /// </summary>
        public void Wait()
        {
            _waitEvent.Wait();

            if (_exception is not null)
            {
                ExceptionDispatchInfo.Throw(_exception);
            }
        }
    }

    // We remove the _actionCount and _resetEvent because now we can track them with our Task objects well enough.

    /// <summary>
    /// The instance method to run as actions in the thread pool. This is a synchronous method.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    /// <param name="taskCompletion">The task completion object to notify completion with.</param>
    public static void InstanceMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        MyTaskCompletion taskCompletion) // New parameter to track the task's completion with
    {
        //Wrap the whole worker method in a try block
        try
        {
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

            // set the task as complete
            taskCompletion.SetResult();
        }
        catch (Exception ex)
        {
            // set the task as complete, but with an error state
            taskCompletion.SetException(ex);
        }
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(CancellationToken cancellationToken)
    {
        int actionCount = 55;
        // Create a list of the tasks to monitor
        List<MyTaskCompletion> tasks = [];
        AsyncLocal<int> mod = new();
        for (int i = 0; i < actionCount; ++i)
        {
            mod.Value = 10 * i;
            string identifier = $"Action {i}";
            // Create a task to send to the instance method to track the completion of the work and add it to the list
            MyTaskCompletion taskCompletion = new();
            ThreadPool.QueueUserWorkItem<ThreadPoolState>(state =>
                InstanceMethod(state.Identifier,
                    1 + state.Mod.Value, 5 + state.Mod.Value,
                    1001 + state.Mod.Value, 1005 + state.Mod.Value,
                    taskCompletion),
                new(identifier, mod), true);
            tasks.Add(taskCompletion);
        }

        // Wait for tall the tasks instead of the reset event
        foreach (MyTaskCompletion task in tasks)
        {
            task.Wait();
        }

        Console.WriteLine("All fin");
    }
}

