using System.Runtime.ExceptionServices;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates making a custom Task class using the standard ThreadPool class.
/// </summary>
public class MyTaskSample : ITutorialSample
{
    // We don't need the old ThreadPoolState any more

    /// <summary>
    /// The custom task class to represent work being done in the thread pool
    /// </summary>
    public class MyTask
    {
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
        /// </summary>
        public void Wait()
        {
            _waitEvent.Wait();

            if (_exception is not null)
            {
                ExceptionDispatchInfo.Throw(_exception);
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
    /// Produces the specified ranges of values.
    /// </summary>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    /// <returns>A list of the produced values</returns>
    public static IEnumerable<int> Produce(
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        for (int value = firstStart; value <= firstEnd; ++value)
        {
            Thread.Sleep(1000);
            yield return value;
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            Thread.Sleep(1000);
            yield return value;
        }
    }

    /// <summary>
    /// Consumes the collection, printing each value to the screen
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The values to print to the screen.</param>
    public static void Consume(
        string identifier,
        IEnumerable<int> values)
    {
        // Remove all the funny tracking we had to add before! We're back to just a normal looking method!
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        foreach (int value in values)
        {
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
        List<MyTask> tasks = [];
        AsyncLocal<int> mod = new();
        for (int index = 1; index <= 55; ++index)
        {
            mod.Value = 10 * index;
            string identifier = $"Action {index}";
            // Now use MyTask.Run to run the simpler method and track it the same!
            tasks.Add(MyTask.Run(() =>
            {
                IEnumerable<int> values = Produce(
                    1 + mod.Value, 5 + mod.Value,
                    1001 + mod.Value, 1005 + mod.Value);
                Consume(identifier, values);
            }));
        }

        foreach (MyTask task in tasks)
        {
            task.Wait();
        }

        Console.WriteLine("All fin");
    }
}
