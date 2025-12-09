/*
 * =====================================================
 *         Step 29 : IObservable?
 * 
 *  We finish up this tutorial with a comparison of channels
 *  to the Observable/Observer pattern. We do this first by
 *  implementing an IObservable and a couple IObservers to
 *  consume our previous values. This shows us the relationship
 *  but also shows us the weaknesses of the standard
 *  IObservable/IObserver interfaces when trying to interact
 *  with async code.
 *  
 *  A.  Copy Step 28. We will update this code.
 *  
 *  B.  Create an Observable<T> : IObservable<T> that takes
 *      an IAsyncEnumerable<T>, and when an IObserver is
 *      subscribe, begins consuming the IAsyncEnumerable,
 *      notifying the observer of each item received.
 *      
 *  C.  Refactor our 2 consumers into IObserver<int>
 *      instances. This looks pretty simple but should
 *      expose places that we no longer have any functionality
 *      to handle (e.g. cancellation, except as Errors).
 *      I add a Task property that comes from a TaskCompletionSource
 *      that is set upon OnComplete or OnError.
 *      This allows me to still have a Task to await on,
 *      but notice that this is not really part of the
 *      Observer pattern by default.
 *      
 *  D.  Update Run to handle the Observer pattern.
 *      This is mostly adding structure to the Run,
 *      especially so we can ensure we clean up the generated
 *      disposal objects, etc.
 *      
 *      
 *  The point of this sample is entirely that a lot of
 *  people ask about the Observable pattern in relation to
 *  the kind of streaming functionality that IAsyncEnumerable
 *  and Channels provides us. However, we see that the
 *  standard IObservable/IObserver are more limited in
 *  what we can do asynchronously.
 *  An interesting note is we always have Await configured
 *  to continue on captured context by this functionality
 *  as well, which we have control over with more asynchronous
 *  code.
 *  Even worse, the IObserver ends up hiding the fact that
 *  all of the notifications are being run on an asynchronous
 *  stack. Any blocking method here could cause a locked
 *  thread on the thread pool. Unknowingly doing many of these
 *  could create thread exhaustion with threads not doing
 *  anything. While we can deal with this as smart engineers,
 *  the hiding of the asynchronous stack is inherently
 *  problematic and we should try to use asynchronous
 *  patterns when we're in asynchronous code.
 * 
 * =====================================================
*/

using System.Threading.Channels;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates how you can turn an IAsyncEnumerable into an IObservable and utilize that to handle all events.
/// </summary>
public class IObservableSample : ITutorialSample
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

    // We add an Observable class that takes an IAsyncEnumerable and consumes it, notifying any observers

    /// <summary>
    /// Observable class that turns IAsyncEnumerable into the IObservable interface
    /// </summary>
    /// <typeparam name="T">The type of messages in the collection.</typeparam>
    /// <param name="source">The source to convert.</param>
    /// <param name="continueOnCapturedContext">If <c>true</c> will capture the current execution context and attempt to return to the same context after await.</param>
    /// <param name="masterTokens">The cancellation token used to signal that a process should not complete. This is extended to all observers that subscribe.</param>
    public class Observable<T>(
        IAsyncEnumerable<T> source)
        : IObservable<T>
    {
        /// <summary>
        /// Simple disposable class that cancels a cancellation token upon disposal
        /// </summary>
        public class CancellationTokenDisposable
            : IDisposable
        {
            /// <summary>
            /// The cancellation source that should be cancelled on disposal
            /// </summary>
            private readonly CancellationTokenSource _cancellationSource = new();

            /// <summary>
            /// Gets the token that is canceled when Dispose is run.
            /// </summary>
            public CancellationToken Token => _cancellationSource.Token;

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                _cancellationSource.Cancel();
                _cancellationSource.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Called when a new value is received, as an asynchronous operation.
        /// </summary>
        /// <param name="observer">The observer to notify of new values and errors.</param>
        /// <param name="value">The value to notify.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        protected virtual async Task OnNewValueAsync(
            IObserver<T> observer, T value, CancellationToken cancellationToken)
        {
            // We don't want to do any real error handling here because we just need to send it up anyway

            observer.OnNext(value);
        }

        /// <summary>
        /// Consumes the enumerable for a observer object.
        /// </summary>
        /// <param name="observer">The observer to consume for.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        private async Task ConsumeForObserver(IObserver<T> observer, CancellationToken cancellationToken)
        {
            try
            {
                await foreach (T value in source.WithCancellation(cancellationToken))
                {
                    await OnNewValueAsync(observer, value, cancellationToken);
                }

                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        }

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        /// <returns>
        /// A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.
        /// </returns>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            CancellationTokenDisposable disposable = new();

            _ = ConsumeForObserver(observer, disposable.Token);

            return disposable;
        }
    }

    /// <summary>
    /// Producer class used to generate integer values and 
    /// </summary>
    public class Producer(int count)
    {
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
    /// Observer class that consumes all events that occur for the async observers
    /// </summary>
    public class Consumer(string identifier) : IObserver<int>
    {
        /// <summary>
        /// The task completion source used to note when this observer has completed operations
        /// </summary>
        private readonly TaskCompletionSource _taskCompletion = new();

        /// <summary>
        /// Gets the task representing the operation of this observer.
        /// </summary>
        public Task Completion => _taskCompletion.Task;

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public void OnCompleted()
        {
            Console.WriteLine($"Fin consumption {identifier} / {Environment.CurrentManagedThreadId}");
            _taskCompletion.SetResult();
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error)
        {
            Console.WriteLine($"Exception in consumption {identifier}: {error.Message}");
            if (error is OperationCanceledException)
            {
                _taskCompletion.SetCanceled();
            }
            else
            {
                _taskCompletion.SetException(error);
            }
        }

        /// <summary>
        /// Provides the observer with new data.
        /// </summary>
        /// <param name="value">The current notification information.</param>
        public void OnNext(int value)
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }
    }

    /// <summary>
    /// Observer class that consumes all events that occur for the async observers
    /// </summary>
    public class Consumer2(string identifier) : IObserver<int>
    {
        /// <summary>
        /// The task completion source used to note when this observer has completed operations
        /// </summary>
        private readonly TaskCompletionSource _taskCompletion = new();

        /// <summary>
        /// Gets the task representing the operation of this observer.
        /// </summary>
        public Task Completion => _taskCompletion.Task;

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public void OnCompleted()
        {
            Console.WriteLine($"Fin modified consumption {identifier} / {Environment.CurrentManagedThreadId}");
            _taskCompletion.SetResult();
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error)
        {
            Console.WriteLine($"Exception in modified consumption {identifier}: {error.Message}");
            if (error is OperationCanceledException)
            {
                _taskCompletion.SetCanceled();
            }
            else
            {
                _taskCompletion.SetException(error);
            }
        }

        /// <summary>
        /// Provides the observer with new data.
        /// </summary>
        /// <param name="value">The current notification information.</param>
        public void OnNext(int value)
        {
            Console.WriteLine($"MODIFIED {identifier} / {Environment.CurrentManagedThreadId} => {value}");
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
        Producer producer = new(producers);
        List<Task> tasks = [];
        SemaphoreSlim synchronize = new(1);

        //We add lists for our observers and disposables
        List<IDisposable> consumerDisposables = [];

        // Get the observable
        Observable<int> observable = new(producer.ReadAllAsync(cancellationToken));

        for (int i = 0; i < consumers; ++i)
        {
            string name = $"Consumer {i}";
            // We need to construct IObserver instances and subscribe instead of running Tasks fire and forget (although that's happening below)
            IObserver<int> consumer;
            if (i % 2 == 0)
            {
                Consumer implementation = new(name);
                tasks.Add(implementation.Completion);
                consumer = implementation;
            }
            else
            {
                Consumer2 implementation = new(name);
                tasks.Add(implementation.Completion);
                consumer = implementation;
            }
            consumerDisposables.Add(observable.Subscribe(consumer));
        }

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
            // have to dispose all these disposables now
            foreach (IDisposable disposable in consumerDisposables)
            {
                disposable.Dispose();
            }
            synchronize.Dispose();
        }
    }
}
