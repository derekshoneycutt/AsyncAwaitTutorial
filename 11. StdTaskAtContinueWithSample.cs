/*
 * =====================================================
 *         Step 11 : Make a chained asynchronous version with Standard Task
 * 
 *  Now we show the same pattern we've developed just using the standard
 *  Task class, with the standard TPL.
 *  
 *  
 *  A.  Copy Step 10. We will reuse all of this.
 *  
 *  B.  Remove MyTask and update InstanceMethod to use just the normal
 *      standard Task object. Have to add Unwrap calls to unwrap
 *      the Task<Task>, which we previously did inside MyTask already.
 *      
 * This is a pretty simple step, just transitioning to the
 * standard Task class now.
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates creating an asynchronous chain of work utilizing the standard Tasks
/// </summary>
public class StdTaskAtContinueWithSample : ITutorialSample
{
    /// <summary>
    /// The instance method to run as tasks.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    public static Task InstanceMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        // Refactor this to use just the standard Task
        // Note the addition of .Unwrap() in multiple places because standard Task returns Task<Task> instead of folding it as we have done in the previous samples
        (int value, int currentEnd) = firstStart <= firstEnd ? (firstStart, firstEnd) : (firstEnd, firstStart);
        Task IncrementAndPrint(int end)
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
            ++value;

            if (value <= end)
            {
                return Task.Delay(1000)
                    .ContinueWith(_ => IncrementAndPrint(end)).Unwrap();
            }
            return Task.CompletedTask;
        }

        Console.WriteLine($"Writing values {identifier} / {Environment.CurrentManagedThreadId}");

        return Task.Delay(1000)
            .ContinueWith(_ => IncrementAndPrint(currentEnd)).Unwrap()
            .ContinueWith(_ =>
            {
                (value, currentEnd) = secondStart <= secondEnd ? (secondStart, secondEnd) : (secondEnd, secondStart);
                return Task.Delay(1000);
            }).Unwrap()
            .ContinueWith(_ => IncrementAndPrint(currentEnd)).Unwrap()
            .ContinueWith(_ => Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}"));
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(CancellationToken cancellationToken)
    {
        int actionCount = 55;
        // Everything in Run must be updated to the standard Task as well
        List<Task> tasks = [];
        AsyncLocal<int> mod = new();
        for (int i = 0; i < actionCount; ++i)
        {
            mod.Value = 10 * i;
            string identifier = $"Action {i}";
            tasks.Add(
                InstanceMethod(identifier,
                    1 + mod.Value, 5 + mod.Value,
                    1001 + mod.Value, 1005 + mod.Value));
        }

        Task.WhenAll(tasks).Wait(cancellationToken);

        Console.WriteLine("All fin");
    }
}
