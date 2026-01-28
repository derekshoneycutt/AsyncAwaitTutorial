/*
 * =====================================================
 *         Step 28 : Structuring and Organizing Channels Code
 * 
 *  Since we have this nice, decoupled channels code, we take
 *  an opportunity to demonstrate one pattern that is often
 *  seen with them, where a producer class produces values into
 *  a private channel and offers a method returning 
 *  IAsyncEnumerable that reads the values from the channel.
 *  
 *  A.  Copy Step 27. We will update this code.
 *  
 *  B.  Create a Producer class and move all of the Producer
 *      related code into it.
 *      Importantly, this will need a private Channel field,
 *      the two private loop production methods, a Run method
 *      to kick off the production of values, and
 *      a ReadAllAsync method to consume the values.
 *      
 *  C.  Update Run to use the Producer class to generate the
 *      production tasks. We'll send in from ReadAllAsync
 *      on the Producer class to the consumers as before.
 *      
 *  D.  (Optional) We add some more exception handling throughout
 *      our consumer methods so that we can have full control
 *      over how they behave in cancellation, exceptions, etc.
 *      
 *      
 *  This doesn't really show anything new, just organizes
 *  the code a little bit easier. However, pay attention to
 *  the pattern of a private field Channel that is never
 *  exposed except as an IAsyncEnumerable for consumption.
 *  This is a common pattern used with async code, which
 *  ensures that the consumer does not have to be aware of
 *  how production is actually happening in any way.
 * 
 * =====================================================
*/

using System.Threading.Channels;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates utilizing Channels in a structured way to demonstrate a stream of values from a central producer class.
/// </summary>
public class StructuredChannelsSample : ITutorialSample
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

    /// <summary>
    /// Producer class used to generate integer values and send them over a channel
    /// </summary>
    public class Producer(int count)
    {
        // We move the channel, the production methods, and a basic Run method into this Producer class.

        /// <summary>
        /// The channel used to communicate the values
        /// </summary>
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

        /// <summary>
        /// Reads all values as an asynchronous collection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A <see cref="IAsyncEnumerable{Int32}"/> that iterates each time a new value is produced.</returns>
        public IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken) =>
            _channel.Reader.ReadAllAsync(cancellationToken);

        /// <summary>
        /// Produces the specified ranges of values.
        /// </summary>
        /// <param name="firstStart">The first start value.</param>
        /// <param name="firstEnd">The first maximum value, completing the first range.</param>
        /// <param name="secondStart">The second start value.</param>
        /// <param name="secondEnd">The second maximum value, completing the second range.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        private async Task Produce(
            int firstStart, int firstEnd, int secondStart, int secondEnd,
            CancellationToken cancellationToken)
        {
            for (int value = firstStart; value <= firstEnd; ++value)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                await _channel.Writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
            }
            for (int value = secondStart; value <= secondEnd; ++value)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                await _channel.Writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Runs the specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        public async Task Run(CancellationToken cancellationToken)
        {
            // For the run, we have basically the same code to launch the producers as before, but now isolated and OOP-ish
            List<Task> productionTasks = [];
            for (int index = 0; index < count; ++index)
            {
                int mod = 10 * index;
                productionTasks.Add(Produce(
                    1 + mod, 5 + mod,
                    1001 + mod, 1005 + mod,
                    cancellationToken));
            }
            await Task.WhenAll(productionTasks).ConfigureAwait(false);
            _channel.Writer.Complete();
        }
    }

    /// <summary>
    /// Consumer class used to print values to the screen
    /// </summary>
    public class Consumer
    {
        /// <summary>
        /// Consumes the collection, printing each value to the screen
        /// </summary>
        /// <param name="identifier">The identifier to print as the name of the current instance.</param>
        /// <param name="values">The values to print to the screen.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        private async Task Consume(
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
        /// Runs the asynchronous.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task Run(
            IAsyncEnumerable<int> values,
            CancellationToken cancellationToken)
        {
            List<Task> consumers = [];
            for (int index = 1; index <= Environment.ProcessorCount * 2; ++index)
            {
                string identifier = $"Action {index}";
                consumers.Add(Consume(identifier, values, cancellationToken));
            }
            await Task.WhenAll(consumers).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        Producer producer = new(55);
        Consumer consumer = new();
        _ = consumer.Run(producer.ReadAllAsync(cancellationToken), cancellationToken);
        
        List<Task> tasks = [producer.Run(cancellationToken)];

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

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);

        Console.WriteLine("All fin");
    }
}
