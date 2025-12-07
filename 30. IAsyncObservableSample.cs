using System.Threading.Channels;

namespace AsyncAwaitTutorial;



/// <summary>
/// Separate static class to house extension method ToObservable for generating an IAsyncObservable from IAsyncEnumerable.
/// See below top comment for explanation of sample.
/// </summary>
public static class IAsyncObservableSampleExtensions
{
    /// <summary>
    /// Converts the IAsyncEnumerable instance to an IAsyncObservable instance.
    /// </summary>
    /// <typeparam name="T">The type of message in the collection</typeparam>
    /// <param name="source">The source to convert.</param>
    /// <param name="continueOnCapturedContext">If <c>true</c> will capture the current execution context and attempt to return to the same context after await.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A new <see cref="IObservable{T}"/> that observes the asynchronous collection.</returns>
    public static IAsyncObservableSample.IAsyncObservable<T> ToAsyncObservable<T>(
        this IAsyncEnumerable<T> source,
        bool continueOnCapturedContext = true,
        CancellationToken cancellationToken = default)
    {
        // Update to IAsyncObservable use instead of IObservable
        return new IAsyncObservableSample.AsyncObservable<T>(source, continueOnCapturedContext, cancellationToken);
    }
}


/// <summary>
/// Samples demonstrating a custom IAsyncObservable and IAsyncObserver definitions and implementations for Asynchronous Observables
/// </summary>
public class IAsyncObservableSample : ITutorialSample
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
    /// Producer class used to generate integer values and 
    /// </summary>
    public class Producer(int count)
    {
        /// <summary>
        /// The channel used to communicate the values
        /// </summary>
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

        /// <summary>
        /// Producers the first range of values to the consumer, with a delay between each production
        /// </summary>
        /// <param name="identifier">The identifier of the producer method to report as.</param>
        /// <param name="channel">The channel to produce values onto.</param>
        /// <param name="start">The start of the range of values to produce.</param>
        /// <param name="end">The end of the range of values to produce.</param>
        /// <param name="secondLoopSignal">The semaphore that signals when the first loop is complete.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        public async Task FirstProducer(
            string identifier,
            int start, int end,
            SemaphoreSlim secondLoopSignal,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"Producing first values: {identifier} / {Environment.CurrentManagedThreadId}");

            for (int i = start; i <= end; ++i)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                await _channel.Writer.WriteAsync(i, cancellationToken).ConfigureAwait(false);
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
        public async Task SecondProducer(
            string identifier,
            int start, int end,
            SemaphoreSlim secondLoopSignal,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"Producing second values: {identifier} / {Environment.CurrentManagedThreadId}");

            await secondLoopSignal.WaitAsync(cancellationToken).ConfigureAwait(false);

            for (int i = start; i <= end; ++i)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                await _channel.Writer.WriteAsync(i, cancellationToken).ConfigureAwait(false);
            }

            Console.WriteLine($"Fin second production {identifier} / {Environment.CurrentManagedThreadId}");
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
                    string action = $"Action {i}";
                    SemaphoreSlim secondLoopSignal = new(0);
                    semaphores.Add(secondLoopSignal);
                    productionTasks.Add(
                        FirstProducer(action, 1 + mod.Value, 5 + mod.Value, secondLoopSignal, cancellationToken));
                    productionTasks.Add(
                        SecondProducer(action, 10001 + mod.Value, 10005 + mod.Value, secondLoopSignal, cancellationToken));
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
        public IAsyncEnumerable<int> ReadAllValuesAsync(CancellationToken cancellationToken) =>
            _channel.Reader.ReadAllAsync(cancellationToken);
    }
    
    /// <summary>
    /// Simple disposable class that cancels a cancellation token upon disposal
    /// </summary>
    public class CancellationTokenDisposable(
        params CancellationToken[] cancellationTokens)
        : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The cancellation source that should be cancelled on disposal
        /// </summary>
        private readonly CancellationTokenSource _cancellationSource =
            cancellationTokens.Length > 0
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens)
                : new();

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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous dispose operation.
        /// </returns>
        public async ValueTask DisposeAsync()
        {
            await _cancellationSource.CancelAsync();
            _cancellationSource.Dispose();
            GC.SuppressFinalize(this);
        }
    }



    /// <summary>
    /// Interface describing an asynchronous observable object
    /// </summary>
    /// <typeparam name="T">The type of data that is being observed in the class.</typeparam>
    public interface IAsyncObservable<out T>
    {
        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        /// <returns>
        /// A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.
        /// </returns>
        ValueTask<IAsyncDisposable> SubscribeAsync(IAsyncObserver<T> observer);
    }

    /// <summary>
    /// Interface describing an asynchronous observer
    /// </summary>
    /// <typeparam name="T">The type of data that is being observed in the class.</typeparam>
    public interface IAsyncObserver<in T>
    {
        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications, as an asynchronous operation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        ValueTask OnCompleted(CancellationToken cancellationToken);

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition, as an asynchronous operation.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        ValueTask OnError(Exception error, CancellationToken cancellationToken);

        /// <summary>
        /// Notifies the observer that the provider has been canceled from further consumption, as an asynchronous operation.
        /// </summary>
        /// <param name="canceledToken">The canceled token causing the cancellation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        ValueTask OnCancel(CancellationToken cancelledToken);

        /// <summary>
        /// Provides the observer with new data, as an asynchronous operation.
        /// </summary>
        /// <param name="value">The current notification information.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        ValueTask OnNext(T value, CancellationToken cancellationToken);
    }



    /// <summary>
    /// Observable class that turns IAsyncEnumerable into the IObservable interface
    /// </summary>
    /// <typeparam name="T">The type of messages in the collection.</typeparam>
    /// <param name="source">The source to convert.</param>
    /// <param name="continueOnCapturedContext">If <c>true</c> will capture the current execution context and attempt to return to the same context after await.</param>
    /// <param name="masterToken">The cancellation token used to signal that a process should not complete. This is extended to all observers that subscribe.</param>
    public class AsyncObservable<T>(
        IAsyncEnumerable<T> source,
        bool continueOnCapturedContext = true,
        CancellationToken masterToken = default)
        : IAsyncObservable<T>
    {
        /// <summary>
        /// Called when a new value is received, as an asynchronous operation.
        /// </summary>
        /// <param name="observer">The observer to notify of new values and errors.</param>
        /// <param name="value">The value to notify.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        protected virtual async Task OnNewValueAsync(
            IAsyncObserver<T> observer, T value, CancellationToken cancellationToken)
        {
            try
            {
                await observer.OnNext(value, cancellationToken).ConfigureAwait(continueOnCapturedContext);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await observer.OnError(ex, cancellationToken).ConfigureAwait(continueOnCapturedContext);
            }
        }

        /// <summary>
        /// Consumes the enumerable for a observer object.
        /// </summary>
        /// <param name="observer">The observer to consume for.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        private async Task ConsumeForObserver(IAsyncObserver<T> observer, CancellationToken cancellationToken)
        {
            try
            {
                await foreach (T value in source.WithCancellation(cancellationToken).ConfigureAwait(continueOnCapturedContext))
                {
                    await OnNewValueAsync(observer, value, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await observer.OnCancel(cancellationToken).ConfigureAwait(continueOnCapturedContext);
            }
            catch (Exception ex)
            {
                await observer.OnError(ex, cancellationToken).ConfigureAwait(continueOnCapturedContext);
            }
            finally
            {
                await observer.OnCompleted(cancellationToken).ConfigureAwait(continueOnCapturedContext);
            }
        }

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        /// <returns>
        /// A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.
        /// </returns>
        public async ValueTask<IAsyncDisposable> SubscribeAsync(IAsyncObserver<T> observer)
        {
            CancellationTokenDisposable disposable = new(masterToken);

            _ = ConsumeForObserver(observer, disposable.Token);

            return disposable;
        }
    }


    /// <summary>
    /// Observer class that consumes all events that occur for the async observers
    /// </summary>
    public class Consumer(string identifier, SemaphoreSlim synchronize) : IAsyncObserver<int>
    {
        /// <summary>
        /// The task completion source used to note when this observer has completed operations
        /// </summary>
        private readonly TaskCompletionSource _taskCompletion = new();

        /// <summary>
        /// Gets the task representing the operation of this observer.
        /// </summary>
        public Task Task => _taskCompletion.Task;

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public async ValueTask OnCompleted(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Fin consumption {identifier} / {Environment.CurrentManagedThreadId}");
            _taskCompletion.SetResult();
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public async ValueTask OnError(Exception error, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Exception in new value for consumption {identifier}: {error.Message}");
        }

        /// <summary>
        /// Notifies the observer that the provider has been canceled from further consumption, as an asynchronous operation.
        /// </summary>
        /// <param name="cancelledToken"></param>
        /// <returns>
        /// A <see cref="ValueTask" /> that represents the asynchronous operation.
        /// </returns>
        public async ValueTask OnCancel(CancellationToken cancelledToken)
        {
            // Add implementation for the OnCancel
            Console.WriteLine($"Consumption was canceled {identifier}");
        }

        /// <summary>
        /// Provides the observer with new data.
        /// </summary>
        /// <param name="value">The current notification information.</param>
        public async ValueTask OnNext(int value, CancellationToken cancellationToken)
        {
            await synchronize.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Console.Write($"Value {identifier} / {Environment.CurrentManagedThreadId}");
                await Task.Yield();
                Console.WriteLine($" / {Environment.CurrentManagedThreadId} : {value}");
            }
            finally
            {
                synchronize.Release();
            }
        }
    }


    /// <summary>
    /// Observer class that consumes all events that occur for the async observers
    /// </summary>
    public class Consumer2(string identifier, SemaphoreSlim synchronize) : IAsyncObserver<int>
    {
        /// <summary>
        /// The task completion source used to note when this observer has completed operations
        /// </summary>
        private readonly TaskCompletionSource _taskCompletion = new();

        /// <summary>
        /// Gets the task representing the operation of this observer.
        /// </summary>
        public Task Task => _taskCompletion.Task;

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public async ValueTask OnCompleted(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Fin modified consumption {identifier} / {Environment.CurrentManagedThreadId}");
            _taskCompletion.SetResult();
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public async ValueTask OnError(Exception error, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Exception in new value for modified consumption {identifier}: {error.Message}");
        }

        /// <summary>
        /// Notifies the observer that the provider has been canceled from further consumption, as an asynchronous operation.
        /// </summary>
        /// <param name="cancelledToken"></param>
        /// <returns>
        /// A <see cref="ValueTask" /> that represents the asynchronous operation.
        /// </returns>
        public async ValueTask OnCancel(CancellationToken cancelledToken)
        {
            // Add implementation for the OnCancel
            Console.WriteLine($"Modified consumption was canceled {identifier}");
        }

        /// <summary>
        /// Provides the observer with new data.
        /// </summary>
        /// <param name="value">The current notification information.</param>
        public async ValueTask OnNext(int value, CancellationToken cancellationToken)
        {
            await synchronize.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Console.Write($"MODIFIED Value {identifier} / {Environment.CurrentManagedThreadId}");
                await Task.Yield();
                Console.WriteLine($" / {Environment.CurrentManagedThreadId} : {value}");
            }
            finally
            {
                synchronize.Release();
            }
        }
    }



    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        int producers = 5;
        int consumers = 9;
        List<Task> productionTasks = [];
        List<Task> tasks = [];
        SemaphoreSlim synchronize = new(1);

        Producer producer = new(producers);
        tasks.Add(producer.Run(cancellationToken));

        // update to async interfaces
        List<IAsyncObserver<int>> consumerObservers = [];
        List<IAsyncDisposable> consumerDisposables = [];

        IAsyncObservable<int> observable = producer.ReadAllValuesAsync(cancellationToken).ToAsyncObservable(false, cancellationToken);

        for (int i = 0; i < consumers; ++i)
        {
            string name = $"Consumer {i}";
            // update to async interfaces
            IAsyncObserver<int> consumer;
            if (i % 2 == 0)
            {
                Consumer implementation = new(name, synchronize);
                tasks.Add(implementation.Task);
                consumer = implementation;
            }
            else
            {
                Consumer2 implementation = new(name, synchronize);
                tasks.Add(implementation.Task);
                consumer = implementation;
            }
            consumerObservers.Add(consumer);
            // have to await this one now
            consumerDisposables.Add(await observable.SubscribeAsync(consumer).ConfigureAwait(false));
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
            // update to handle async
            foreach (IAsyncDisposable disposable in consumerDisposables)
            {
                await disposable.DisposeAsync();
            }
            synchronize.Dispose();
        }
    }
}
