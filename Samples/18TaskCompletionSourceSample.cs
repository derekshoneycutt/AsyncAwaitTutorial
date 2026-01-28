/*
 * =====================================================
 *         Step 18 : Standard TaskCompletionSource
 * 
 *  We bring back the TaskCompletion pattern that we have reused
 *  repeatedly with paralleling our work in Step 6 along our async
 *  code, but using the standard TaskCompletionSource now.
 *  TaskCompletionSource can be used for many purposes, but
 *  perhaps a background thread running a long-running operation
 *  could be one.
 *  
 *  A.  Rebuild DoubleLoop that we started with, but now
 *      track it with the standard TaskCompletionSource,
 *      using the same pattern used through the custom Task
 *      implementation.
 *      
 *  B.  Update Run to launch this thread and add the Task
 *      from the TaskCompletionSource to the list of Tasks
 *      that are awaited at the end.
 *      
 *      
 * This is entirely familiar, but it shows us how we can
 * manage long running processes on our own thread and
 * offer a handle to wait on it asynchronously. This can be
 * a cheap and useful means of coordinating asynchronous code,
 * using a pattern we have repeated extensively.
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates how to utilize a TaskCompletionSource to expand asynchronous code
/// </summary>
public class TaskCompletionSourceSample : ITutorialSample
{
    /// <summary>
    /// The instance method to run as independent threads in the sample. This is a synchronous method.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    /// <param name="completionSource">The Task Completion Source to mark when this task has completed</param>
    public static void DoubleLoop(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        TaskCompletionSource completionSource)
    {
        // Almost identical to step 1's DoubleLoop, but completionSource is a TaskCompletionSource.
        try
        {
            Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

            for (int value = firstStart; value <= firstEnd; ++value)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
            }
            for (int value = secondStart; value <= secondEnd; ++value)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
            }

            Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");

            completionSource.SetResult();
        }
        catch (Exception ex)
        {
            completionSource.SetException(ex);
        }
    }

    /// <summary>
    /// Delays for a second and then returns a given number as an asynchronous operation.
    /// </summary>
    /// <param name="number">The number to return.</param>
    /// <returns>A <see cref="Task{Int32}"/> that represents the asynchronous operation. <c>Result</c> contains the specified integer.</returns>
    public static async Task<int> DelayOnNumber(
        int number)
    {
        await Task.Delay(1000).ConfigureAwait(false);
        return number;
    }

    /// <summary>
    /// Produces the specified ranges of values.
    /// </summary>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    /// <returns>A list of the produced values</returns>
    public static IEnumerable<Task<int>> Produce(
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        for (int value = firstStart; value <= firstEnd; ++value)
        {
            yield return DelayOnNumber(value);
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            yield return DelayOnNumber(value);
        }
    }

    /// <summary>
    /// Consumes the collection, printing each value to the screen
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The values to print to the screen.</param>
    public static async Task Consume(
        string identifier,
        IEnumerable<Task<int>> values)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        foreach (Task<int> valueTask in values)
        {
            int value = await valueTask.ConfigureAwait(false);
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
        List<Task> tasks = [];
        AsyncLocal<int> mod = new();
        for (int index = 1; index <= 55; ++index)
        {
            mod.Value = 10 * index;
            string identifier = $"Action {index}";
            IEnumerable<Task<int>> values = Produce(
                1 + mod.Value, 5 + mod.Value,
                1001 + mod.Value, 1005 + mod.Value);
            tasks.Add(Consume(identifier, values));
        }

        // We delay a short time and then spin off a background thread, with a ThreadCompletionSource to track its progress.
        // the Thread from the ThreadCompletionSource is added to the tasks lists to wait on.
        await Task.Delay(500).ConfigureAwait(false);
        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            DoubleLoop("Single Thread",
                1, 5,
                101, 105,
                backThreadSource)));
        instanceCaller.Start();
        tasks.Add(backThreadSource.Task);

        await Task.WhenAll(tasks).ConfigureAwait(false);

        Console.WriteLine("All fin");
    }
}
