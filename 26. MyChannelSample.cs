using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates a custom, basic channel class that shows the basic motivations and how it works
/// </summary>
/// <remarks>
/// The goal of this sample is to transition to using Channels for asynchronous communications.
/// A very simple, custom Channel class is constructed and utilized to create the producer/consumer pattern in code.
/// In transitioning to using the Channels, we move the decoupling of values from the consumption side to the production side.
/// This feels like somewhat of a reverse from the previous sample but creates a more logical decoupling on the producer/consumer pattern.
/// We do also setup 2 separate consumers to show that it is possible to have multiple consumers and producers.
/// </remarks>
public class MyChannelSample : ITutorialSample
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

            for (int i = firstStart; i <= firstEnd; i++)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
            }
            for (int i = secondStart; i <= secondEnd; i++)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
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
    /// Custom channels class used to send messages from producers to consumers
    /// </summary>
    /// <typeparam name="T">The message type to communicate</typeparam>
    public class MyChannel<T>
    {
        /// <summary>
        /// The queue of messages to read
        /// </summary>
        private readonly ConcurrentQueue<T> _queue = new();

        /// <summary>
        /// The semaphore that is used to signal consumers that a new item is available.
        /// </summary>
        private readonly SemaphoreSlim _semaphore = new(0);

        /// <summary>
        /// Flag indicating if this instance has been completed and no mroe messages should be sent.
        /// </summary>
        private bool _completed = false;

        /// <summary>
        /// Writes the specified value from the consumer.
        /// </summary>
        public void Write(T value)
        {
            _queue.Enqueue(value);
            _semaphore.Release();
        }

        /// <summary>
        /// Completes this instance.
        /// </summary>
        public void Complete()
        {
            _completed = true;
            _semaphore.Release();
        }

        /// <summary>
        /// Reads all messages that are available as an asynchronous collection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>An asynchronous collection that iterates each time a new message is available.</returns>
        public async IAsyncEnumerable<T> ReadAllAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (!_completed)
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (!_completed && _queue.TryDequeue(out T? next) && (next is not null))
                {
                    yield return next;
                }
                else if (_completed)
                {
                    _semaphore.Release();
                }
            }
        }
    }




    /// <summary>
    /// Producers the first range of values to the consumer, with a delay between each production
    /// </summary>
    /// <param name="identifier">The identifier of the producer method to report as.</param>
    /// <param name="channel">The channel to produce values onto.</param>
    /// <param name="start">The start of the range of values to produce.</param>
    /// <param name="end">The end of the range of values to produce.</param>
    /// <param name="secondLoopSignal">The semaphore that signals when the first loop is complete.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task FirstProducer(
        string identifier,
        MyChannel<int> channel,
        int start, int end,
        SemaphoreSlim secondLoopSignal,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Producing first values: {identifier} / {Environment.CurrentManagedThreadId}");

        for (int i = start; i <= end; ++i)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            channel.Write(i);
        }

        secondLoopSignal.Release();

        Console.WriteLine($"Fin first production {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Producers the second range of values to the consumer, with a delay between each production
    /// </summary>
    /// <param name="identifier">The identifier of the producer method to report as.</param>
    /// <param name="channel">The channel to produce values onto.</param>
    /// <param name="start">The start of the range of values to produce.</param>
    /// <param name="end">The end of the range of values to produce.</param>
    /// <param name="secondLoopSignal">The semaphore that signals when the first loop is complete.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task SecondProducer(
        string identifier,
        MyChannel<int> channel,
        int start, int end,
        SemaphoreSlim secondLoopSignal,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Producing second values: {identifier} / {Environment.CurrentManagedThreadId}");

        await secondLoopSignal.WaitAsync(cancellationToken).ConfigureAwait(false);

        for (int i = start; i <= end; ++i)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            channel.Write(i);
        }

        Console.WriteLine($"Fin second production {identifier} / {Environment.CurrentManagedThreadId}");
    }


    /// <summary>
    /// Called when a new value is received in one of the main loops.
    /// </summary>
    /// <param name="identifier">The identifier of the action to print the value for.</param>
    /// <param name="value">The value to print.</param>
    /// <param name="synchronize">The semaphore used to synchronize console output.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    private static async Task OnNewValue(
        string identifier,
        int value,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
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

    /// <summary>
    /// Called when a new value is received in one of the main loops.
    /// </summary>
    /// <param name="identifier">The identifier of the action to print the value for.</param>
    /// <param name="value">The value to print.</param>
    /// <param name="synchronize">The semaphore used to synchronize console output.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    private static async Task OnNewValue2(
        string identifier,
        int value,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        // We create a second OnNewValue in order to have a modified handling (we just throw MODIFIED in the console text)
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


    /// <summary>
    /// Loops over the integers received from the channel and prints them, as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="channel"></param>
    /// <param name="synchronize">The semaphore used to synchronize printing values to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task Consumer(
        string identifier,
        MyChannel<int> channel,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        // The two loops are now separated on the production side, so we just need to loop through all values we receive and handle them
        Console.WriteLine($"Consuming values: {identifier} / {Environment.CurrentManagedThreadId}");

        await foreach (int value in channel.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            await OnNewValue2(identifier, value, synchronize, cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"Fin consuming {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Loops over the integers received from the channel and prints them with slight modification, as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="channel"></param>
    /// <param name="synchronize">The semaphore used to synchronize printing values to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task Consumer2(
        string identifier,
        MyChannel<int> channel,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        // We add a second consumer to demonstrate multiple consumers can be working on the produced data
        // The two loops are now separated on the production side, so we just need to loop through all values we receive and handle them
        Console.WriteLine($"Second consuming values: {identifier} / {Environment.CurrentManagedThreadId}");

        await foreach (int value in channel.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            await OnNewValue(identifier, value, synchronize, cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"Fin second consuming {identifier} / {Environment.CurrentManagedThreadId}");
    }


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        // change our action count to producers and consumers, and create the channel for comms.
        int producers = 5;
        int consumers = 9;
        MyChannel<int> channel = new();
        // We'll need a special productionTask list in order to complete the channel
        List<Task> productionTasks = [];
        List<Task> tasks = [];
        SemaphoreSlim synchronize = new(1);
        List<SemaphoreSlim> semaphores = [synchronize];
        AsyncLocal<int> mod = new();
        for (int i = 0; i < producers; ++i)
        {
            mod.Value = 10 * i;
            string action = $"Action {i}";
            SemaphoreSlim secondLoopSignal = new(0);
            semaphores.Add(secondLoopSignal);
            //Add each of the producer tasks to productionTasks instead of tasks for now
            productionTasks.Add(
                FirstProducer(action, channel, 1 + mod.Value, 5 + mod.Value, secondLoopSignal, cancellationToken));
            productionTasks.Add(
                SecondProducer(action, channel, 10001 + mod.Value, 10005 + mod.Value, secondLoopSignal, cancellationToken));
        }

        // Complete the channel when all production is complete and add the final task to the final wait list
        tasks.Add(Task.WhenAll(productionTasks)
            .ContinueWith(_ =>
            {
                channel.Complete();
            }, cancellationToken));

        // Create the consumers, and add them as tasks to the final wait list
        for (int i = 0; i < consumers; ++i)
        {
            string name = $"Consumer {i}";
            if (i % 2 == 0)
            {
                _ = Consumer(name, channel, synchronize, cancellationToken);
            }
            else
            {
                _ = Consumer2(name, channel, synchronize, cancellationToken);
            }
        }


        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            ThreadMethod("Single Thread", 1, 5, 101, 105, backThreadSource, cancellationToken)));
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
            foreach (SemaphoreSlim semaphore in semaphores)
            {
                semaphore.Dispose();
            }
        }
    }
}
