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
    public static void ThreadMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        TaskCompletionSource completionSource,
        CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

            (int start, int end) = firstStart <= firstEnd ? (firstStart, firstEnd) : (firstEnd, firstStart);
            for (int value = start; value <= end; ++value)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
            }
            (start, end) = secondStart <= secondEnd ? (secondStart, secondEnd) : (secondEnd, secondStart);
            for (int value = start; value <= end; ++value)
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
    /// Producer class used to generate integer values and 
    /// </summary>
    public class Producer(int count)
    {
        // We move the channel, the production methods, and a basic Run method into this Producer class.

        /// <summary>
        /// The channel used to communicate the values
        /// </summary>
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

        /// <summary>
        /// Produces the first range of values to the consumer, with a delay between each production
        /// </summary>
        /// <param name="identifier">The identifier of the producer method to report as.</param>
        /// <param name="channel">The channel to produce values onto.</param>
        /// <param name="start">The start of the range of values to produce.</param>
        /// <param name="end">The end of the range of values to produce.</param>
        /// <param name="secondLoopSignal">The semaphore that signals when the first loop is complete.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        private async Task FirstLoop(
            string identifier,
            int start, int end,
            SemaphoreSlim secondLoopSignal,
            CancellationToken cancellationToken)
        {
            // Use the field _channel instead of passing one in here

            Console.WriteLine($"Writing first values: {identifier} / {Environment.CurrentManagedThreadId}");

            (int doStart, int doEnd) = start <= end ? (start, end) : (end, start);
            for (int value = doStart; value <= doEnd; ++value)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                await _channel.Writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
            }

            secondLoopSignal.Release();

            Console.WriteLine($"Fin first values {identifier} / {Environment.CurrentManagedThreadId}");
        }

        /// <summary>
        /// Produces the second range of values to the consumer, with a delay between each production
        /// </summary>
        /// <param name="identifier">The identifier of the producer method to report as.</param>
        /// <param name="channel">The channel to produce values onto.</param>
        /// <param name="start">The start of the range of values to produce.</param>
        /// <param name="end">The end of the range of values to produce.</param>
        /// <param name="secondLoopSignal">The semaphore that signals when the first loop is complete.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        private async Task SecondLoop(
            string identifier,
            int start, int end,
            SemaphoreSlim secondLoopSignal,
            CancellationToken cancellationToken)
        {
            // Use the field _channel instead of passing one in here

            Console.WriteLine($"Writing second values: {identifier} / {Environment.CurrentManagedThreadId}");

            await secondLoopSignal.WaitAsync(cancellationToken).ConfigureAwait(false);

            (int doStart, int doEnd) = start <= end ? (start, end) : (end, start);
            for (int value = doStart; value <= doEnd; ++value)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                await _channel.Writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
            }

            Console.WriteLine($"Fin second values {identifier} / {Environment.CurrentManagedThreadId}");
        }

        /// <summary>
        /// Runs the specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        public async Task Run(CancellationToken cancellationToken)
        {
            // For the run, we have basically the same code to launch the producers as before, but now isolated and OOP-ish
            List<Task> productionTasks = [];
            List<SemaphoreSlim> semaphores = [];
            AsyncLocal<int> mod = new();

            try
            {
                for (int i = 0; i < count; ++i)
                {
                    mod.Value = 10 * i;
                    string identifier = $"Action {i}";
                    SemaphoreSlim secondLoopSignal = new(0);
                    semaphores.Add(secondLoopSignal);
                    productionTasks.Add(
                        FirstLoop(identifier,
                            1 + mod.Value, 5 + mod.Value,
                            secondLoopSignal, cancellationToken));
                    productionTasks.Add(
                        SecondLoop(identifier,
                            1001 + mod.Value, 1005 + mod.Value,
                            secondLoopSignal, cancellationToken));
                }

                await Task.WhenAll(productionTasks).ConfigureAwait(false);

                _channel.Writer.Complete();
            }
            finally
            {
                foreach (SemaphoreSlim semaphore in semaphores)
                {
                    semaphore.Dispose();
                }
            }
        }

        /// <summary>
        /// Reads all values as an asynchronous collection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A <see cref="IAsyncEnumerable{Int32}"/> that iterates each time a new value is produced.</returns>
        public IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken) =>
            _channel.Reader.ReadAllAsync(cancellationToken);
    }


    /// <summary>
    /// Called when a new value is received in one of the main loops.
    /// </summary>
    /// <param name="identifier">The identifier of the action to print the value for.</param>
    /// <param name="value">The value to print.</param>
    /// <param name="synchronize">The semaphore used to synchronize console output.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    private static async Task OnNewValueAsync(
        string identifier,
        int value,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        // Add some extra exception handling for good practice
        try
        {
            await synchronize.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Console.Write($"{identifier} / {Environment.CurrentManagedThreadId}");
                await Task.Yield();
                Console.WriteLine($" / {Environment.CurrentManagedThreadId} => {value}");
            }
            finally
            {
                synchronize.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"Consumption of value cancelled {identifier} {value}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in consumption of value {identifier} {value}: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when a new value is received in one of the main loops.
    /// </summary>
    /// <param name="identifier">The identifier of the action to print the value for.</param>
    /// <param name="value">The value to print.</param>
    /// <param name="synchronize">The semaphore used to synchronize console output.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    private static async Task OnNewValue2Async(
        string identifier,
        int value,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        // Add some extra exception handling for good practice
        try
        {
            await synchronize.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Console.Write($"MODIFIED {identifier} / {Environment.CurrentManagedThreadId}");
                await Task.Yield();
                Console.WriteLine($" / {Environment.CurrentManagedThreadId} => {value}");
            }
            finally
            {
                synchronize.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"Consumption of value2 cancelled {identifier} {value}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in consumption of value2 {identifier} {value}: {ex.Message}");
        }
    }

    /// <summary>
    /// Loops over the integers received from the channel and prints them, as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The asynchronous collection of values to read</param>
    /// <param name="synchronize">The semaphore used to synchronize printing values to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task Consumer(
        string identifier,
        IAsyncEnumerable<int> values,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        // Add some extra exception handling for good practice
        try
        {
            Console.WriteLine($"Writing consumer: {identifier} / {Environment.CurrentManagedThreadId}");

            await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await OnNewValueAsync(identifier, value, synchronize, cancellationToken).ConfigureAwait(false);
            }

            Console.WriteLine($"Fin consumer {identifier} / {Environment.CurrentManagedThreadId}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"Consumption cancelled {identifier}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in consumption {identifier}: {ex.Message}");
        }
    }

    /// <summary>
    /// Loops over the integers received from the channel and prints them with slight modification, as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The asynchronous collection of values to read</param>
    /// <param name="synchronize">The semaphore used to synchronize printing values to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task Consumer2(
        string identifier,
        IAsyncEnumerable<int> values,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        // Add some extra exception handling for good practice
        try
        {
            Console.WriteLine($"Writing second consumer: {identifier} / {Environment.CurrentManagedThreadId}");

            await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await OnNewValue2Async(identifier, value, synchronize, cancellationToken).ConfigureAwait(false);
            }

            Console.WriteLine($"Fin second consumer {identifier} / {Environment.CurrentManagedThreadId}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"Consumption2 cancelled {identifier}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in consumption2 {identifier}: {ex.Message}");
        }
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        int producers = 99;
        int consumers = Environment.ProcessorCount * 2;
        // We just create the Producer class instead of channel and list, etc. we had before
        Producer producer = new(producers);
        List<Task> tasks = [];
        SemaphoreSlim synchronize = new(1);

        for (int i = 0; i < consumers; ++i)
        {
            string name = $"Consumer {i}";
            // Read values from the Producer class instead of the channel directly
            if (i % 2 == 0)
            {
                _ = Consumer(name, producer.ReadAllAsync(cancellationToken), synchronize, cancellationToken);
            }
            else
            {
                _ = Consumer2(name, producer.ReadAllAsync(cancellationToken), synchronize, cancellationToken);
            }
        }

        // And run the producers
        tasks.Add(producer.Run(cancellationToken));

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            ThreadMethod("Single Thread",
                1, 5,
                101, 105,
                backThreadSource, cancellationToken)));
        instanceCaller.Start();
        tasks.Add(backThreadSource.Task);

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);

            Console.WriteLine("All fin");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("Canceled");
        }
        finally
        {
            // only the synchronize semaphore is left to dispose
            synchronize.Dispose();
        }
    }
}
