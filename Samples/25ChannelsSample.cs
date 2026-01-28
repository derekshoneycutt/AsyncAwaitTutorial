/*
 * =====================================================
 *         Step 27 : Standard Channels
 * 
 *  Now that we have a good concept of Channels from making a cheap
 *  version of our own, we switch over to the much more featureful
 *  standard Channels structures. Overall, our code remains the same,
 *  with just a few tweaks.
 *  
 *  A.  Copy Step 26. We will update this code.
 *  
 *  B.  Remove the custom Channel implementation and replace all
 *      references with the standard channel implementation.
 *      We will just use an Unbounded channel for now,
 *      and our Producers will receive ChannelWriter instead of
 *      the whole thing--maintaining our separation of concerns.
 *      
 *      
 *  We can now make robust asynchronous code and utilize
 *  Channels to decouple our producers and consumers, allowing
 *  for multiple of either, and a great deal of control over both.
 * 
 * =====================================================
*/

using System.Threading.Channels;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates utilizing the standard Channels in Producer/Consumer asynchronous pattern.
/// </summary>
public class ChannelsSample : ITutorialSample
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
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static void DoubleLoop(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        TaskCompletionSource completionSource,
        CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

            for (int value = firstStart; value <= firstEnd; ++value)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
            }
            for (int value = secondStart; value <= secondEnd; ++value)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");

            completionSource.SetResult();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            completionSource.SetCanceled(cancellationToken);
        }
        catch (Exception ex)
        {
            completionSource.SetException(ex);
        }
    }

    // No more custom Channels class; note how much we use ChannelReader and ChannelWriter instead now!

    /// <summary>
    /// Produces the specified ranges of values.
    /// </summary>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task Produce(
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        ChannelWriter<int> channel,
        CancellationToken cancellationToken)
    {
        // Update to the standard channel writer
        for (int value = firstStart; value <= firstEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            await channel.WriteAsync(value, cancellationToken).ConfigureAwait(false);
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            await channel.WriteAsync(value, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Consumes the collection, printing each value to the screen
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The values to print to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task Consume(
        string identifier,
        IAsyncEnumerable<int> values,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        // change to the standard channels structure
        Channel<int> channel = Channel.CreateUnbounded<int>();

        for (int index = 1; index <= Environment.ProcessorCount * 2; ++index)
        {
            string identifier = $"Action {index}";
            _ = Consume(identifier, channel.Reader.ReadAllAsync(cancellationToken), cancellationToken);
        }

        List<Task> tasks = [];
        for (int index = 1; index <= 55; ++index)
        {
            int mod = 10 * index;
            string identifier = $"Action {index}";
            tasks.Add(Produce(
                1 + mod, 5 + mod,
                1001 + mod, 1005 + mod,
                channel,
                cancellationToken));
        }

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            DoubleLoop("Single Thread",
                1, 5,
                101, 105,
                backThreadSource, cancellationToken)));
        instanceCaller.Start();
        tasks.Add(backThreadSource.Task);

        await Task.WhenAll(tasks).ConfigureAwait(false);
        channel.Writer.Complete();

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);

        Console.WriteLine("All fin");
    }
}
