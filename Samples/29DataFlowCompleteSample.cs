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
using System.Threading.Tasks.Dataflow;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates utilizing Channels in a structured way to demonstrate a stream of values from a central producer class.
/// </summary>
public class DataFlowCompleteSample : ITutorialSample
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
    public class Producer(int count, ITargetBlock<int> targetBlock)
    {
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
                await targetBlock.SendAsync(value, cancellationToken).ConfigureAwait(false);
            }
            for (int value = secondStart; value <= secondEnd; ++value)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                await targetBlock.SendAsync(value, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Runs the specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        public async Task Run(CancellationToken cancellationToken)
        {
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
        }
    }



    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        DataflowBlockOptions blockOptions = new()
        {
            CancellationToken = cancellationToken,
            BoundedCapacity = Environment.ProcessorCount * 2,
            MaxMessagesPerTask = 5
        };

        GroupingDataflowBlockOptions groupingOptions = new()
        {
            CancellationToken = cancellationToken,
            BoundedCapacity = Environment.ProcessorCount * 2,
            Greedy = true,
            MaxMessagesPerTask = 5
        };

        ExecutionDataflowBlockOptions executionOptions = new()
        {
            CancellationToken = cancellationToken,
            BoundedCapacity = Environment.ProcessorCount * 2,
            SingleProducerConstrained = false,
            MaxMessagesPerTask = 5,
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2
        };

        BufferBlock<int> buffer = new(blockOptions);

        BatchBlock<int> batcher = new(2, groupingOptions);
        buffer.LinkTo(batcher);

        TransformBlock<int[], int> transform = new(values =>
            (100000 * values[0]) + (values.Length > 0 ? values[1] : 0), executionOptions);
        batcher.LinkTo(transform);

        BroadcastBlock<int> broadcast = new(null, blockOptions);
        transform.LinkTo(broadcast);

        ActionBlock<int> writer = new(value =>
            Console.WriteLine($"Writer 1 / {Environment.CurrentManagedThreadId} => {value}", executionOptions));
        broadcast.LinkTo(writer);

        ActionBlock<int> writer2 = new(value =>
            Console.WriteLine($"Writer 2 / {Environment.CurrentManagedThreadId} => {value}", executionOptions));
        broadcast.LinkTo(writer2);

        ActionBlock<int> writer3 = new(value =>
            Console.WriteLine($"Writer 3 / {Environment.CurrentManagedThreadId} => {value}", executionOptions));
        broadcast.LinkTo(writer3);

        Producer producer = new(55, buffer);
        
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

        buffer.Complete();
        batcher.Complete();
        transform.Complete();
        broadcast.Complete();
        writer.Complete();
        writer2.Complete();
        writer3.Complete();

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);

        Console.WriteLine("All fin");
    }
}
