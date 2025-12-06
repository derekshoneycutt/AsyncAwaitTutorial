using System.Runtime.ExceptionServices;

namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates creating a basic task completion class to track tasks on the thread pool
/// </summary>
public static class MyTaskCompletionSamples
{
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
        private readonly ManualResetEventSlim _waitEvent = new();


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
                if (IsCompleted)
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

    /// <summary>
    /// The instance method to run as actions in the thread pool. This is a synchronous method.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstMax">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondMax">The second maximum value, completing the second range.</param>
    public static void InstanceMethod(
        string identifier,
        int firstStart, int firstMax, int secondStart, int secondMax,
        MyTaskCompletion taskCompletion)
    {
        try
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

            taskCompletion.SetResult();
        }
        catch (Exception ex)
        {
            taskCompletion.SetException(ex);
        }
    }


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    public static void Run()
    {
        int actionCount = 55;
        List<MyTaskCompletion> tasks = new();
        for (int i = 0; i < actionCount; ++i)
        {
            int mod = 10 * i;
            string action = $"Action {i}";
            MyTaskCompletion taskCompletion = new();
            ThreadPool.QueueUserWorkItem(_ => InstanceMethod(
                action, 1 + mod, 5 + mod, 10001 + mod, 10005 + mod,
                taskCompletion));
            tasks.Add(taskCompletion);
        }

        foreach (MyTaskCompletion task in tasks)
        {
            task.Wait();
        }

        Console.WriteLine("All fin");
    }
}

