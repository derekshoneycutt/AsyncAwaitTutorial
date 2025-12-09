/*
 * =====================================================
 *         Step 30 : IAsyncObservable?
 * 
 *  Since we noticed some issues with the IObservable/IObserver
 *  pattern in our last sample, we will try to theoretically
 *  correct some of them here by creating an async version of
 *  the pattern with a significant amount of the benefit of
 *  the async pattern returned. This is mostly just for fun,
 *  as it is a lot of work to add onto an already pretty
 *  strong pattern. However, it brings together a lot of the
 *  prior samples to a nice concluding point, whether
 *  this is a theoretically useful (and correct strategy
 *  there upon for that matter) or not.
 *  
 *  A.  Copy Step 29. We will update this code.
 *  
 *  B.  Construct IAsyncObservable<out T> and IAsyncObserver<in T>.
 *      We'll make everything async with ValueTask to ensure
 *      efficiency when necessary, and we'll pass cancellation
 *      tokens everywhere as well.
 *      We want to consider that we may or may not want to run
 *      on the captured context as well.
 *      
 *  C.  Updated the Observable and Observers to the new
 *      async pattern. Update Run accordingly.
 *      The Observer can add a lot of special exception handling
 *      for cancellation and specific error conditions related
 *      to the stream functionality.
 *      
 *      
 *  The interface created here is not going to make everyone
 *  happy, but it attempts to counteract any critiques or
 *  concerns that were present in the the last sample.
 *  Whether anyone ever uses this is beside the point,
 *  we still get to see how the async streams with
 *  IAsyncEnumerable and Channels compares and interacts with
 *  the common Observable/Observer pattern, even as we 
 *  stretch the latter to its limits.
 * 
 * =====================================================
*/

using System.Threading.Channels;

namespace AsyncAwaitTutorial;

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
    /// Interface describing an asynchronous observable object
    /// </summary>
    /// <typeparam name="T">The type of data that is being observed in the class.</typeparam>
    public interface IAsyncObservable<out T>
    {
        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        /// <param name="continueOnCapturedContext">If <c>true</c> will capture the current execution context and attempt to return to the same context after await.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>
        /// A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.
        /// </returns>
        ValueTask<IAsyncDisposable> SubscribeAsync(
            IAsyncObserver<T> observer,
            bool continueOnCapturedContext = true,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface describing an asynchronous observer
    /// </summary>
    /// <typeparam name="T">The type of data that is being observed in the class.</typeparam>
    public interface IAsyncObserver<in T>
    {
        /// <summary>
        /// Gets a task that represents the observer's activity, completing on completed, error, or cancellation
        /// </summary>
        Task Completion { get; }

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications, as an asynchronous operation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        ValueTask OnCompletedAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition, as an asynchronous operation.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        ValueTask OnErrorAsync(Exception error, CancellationToken cancellationToken);

        /// <summary>
        /// Notifies the observer that the provider has been canceled from further consumption, as an asynchronous operation.
        /// </summary>
        /// <param name="canceledToken">The canceled token causing the cancellation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        ValueTask OnCanceledAsync(CancellationToken canceledToken);

        /// <summary>
        /// Provides the observer with new data, as an asynchronous operation.
        /// </summary>
        /// <param name="value">The current notification information.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        ValueTask OnNextAsync(T value, CancellationToken cancellationToken);

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition while handling a next value, as an asynchronous operation.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation. <c>Result</c> should contain a boolean flag; if <c>true</c>, the error will be propagated up the chain and will be received in OnErrorAsync as well.</returns>
        ValueTask<bool> OnNextErrorAsync(Exception error, CancellationToken cancellationToken);
    }



    /// <summary>
    /// Observable class that turns IAsyncEnumerable into the IObservable interface
    /// </summary>
    /// <typeparam name="T">The type of messages in the collection.</typeparam>
    /// <param name="source">The source to convert.</param>
    /// <param name="continueOnCapturedContext">If <c>true</c> will capture the current execution context and attempt to return to the same context after await.</param>
    /// <param name="masterToken">The cancellation token used to signal that a process should not complete. This is extended to all observers that subscribe.</param>
    public class AsyncObservable<T>(
        IAsyncEnumerable<T> source)
        : IAsyncObservable<T>
    {
        /// <summary>
        /// Simple disposable class that cancels a cancellation token upon disposal
        /// </summary>
        public class CancellationTokenDisposable
            : IDisposable, IAsyncDisposable
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
        /// Called when a new value is received, as an asynchronous operation.
        /// </summary>
        /// <param name="observer">The observer to notify of new values and errors.</param>
        /// <param name="value">The value to notify.</param>
        /// <param name="continueOnCapturedContext">If <c>true</c> will capture the current execution context and attempt to return to the same context after await.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        protected virtual async Task OnNewValueAsync(
            IAsyncObserver<T> observer, T value,
            bool continueOnCapturedContext,
            CancellationToken cancellationToken)
        {
            try
            {
                await observer.OnNextAsync(value, cancellationToken).ConfigureAwait(continueOnCapturedContext);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (await observer.OnNextErrorAsync(ex, cancellationToken).ConfigureAwait(continueOnCapturedContext))
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Consumes the enumerable for a observer object.
        /// </summary>
        /// <param name="observer">The observer to consume for.</param>
        /// <param name="continueOnCapturedContext">If <c>true</c> will capture the current execution context and attempt to return to the same context after await.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        private async Task ConsumeForObserver(
            IAsyncObserver<T> observer,
            bool continueOnCapturedContext,
            CancellationToken cancellationToken)
        {
            try
            {
                await foreach (T value in source.WithCancellation(cancellationToken).ConfigureAwait(continueOnCapturedContext))
                {
                    await OnNewValueAsync(observer, value, continueOnCapturedContext, cancellationToken).ConfigureAwait(continueOnCapturedContext);
                }

                await observer.OnCompletedAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await observer.OnCanceledAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext);
            }
            catch (Exception ex)
            {
                await observer.OnErrorAsync(ex, cancellationToken).ConfigureAwait(continueOnCapturedContext);
            }
        }

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        /// <param name="continueOnCapturedContext">If <c>true</c> will capture the current execution context and attempt to return to the same context after await.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>
        /// A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.
        /// </returns>
        public async ValueTask<IAsyncDisposable> SubscribeAsync(
            IAsyncObserver<T> observer,
            bool continueOnCapturedContext = true,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CancellationTokenDisposable disposable = new();

            _ = ConsumeForObserver(observer, continueOnCapturedContext, disposable.Token);

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
    public class Consumer(
        string identifier, SemaphoreSlim synchronize)
        : IAsyncObserver<int>
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
        public async ValueTask OnCompletedAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Fin consumption {identifier} / {Environment.CurrentManagedThreadId}");
            _taskCompletion.SetResult();
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public async ValueTask OnErrorAsync(Exception error, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Exception in consumption {identifier}: {error.Message}");
            _taskCompletion.SetException(error);
        }

        /// <summary>
        /// Notifies the observer that the provider has been canceled from further consumption, as an asynchronous operation.
        /// </summary>
        /// <param name="canceledToken">The cancellation token that was cancelled during consumption.</param>
        /// <returns>
        /// A <see cref="ValueTask" /> that represents the asynchronous operation.
        /// </returns>
        public async ValueTask OnCanceledAsync(CancellationToken canceledToken)
        {
            Console.WriteLine($"Consumption was canceled {identifier}");
            _taskCompletion.SetCanceled(canceledToken);
        }

        /// <summary>
        /// Provides the observer with new data.
        /// </summary>
        /// <param name="value">The current notification information.</param>
        public async ValueTask OnNextAsync(int value, CancellationToken cancellationToken)
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

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition while handling a next value, as an asynchronous operation.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation. <c>Result</c> should contain a boolean flag; if <c>true</c>, the error will be propagated up the chain and will be received in OnErrorAsync as well.</returns>
        public async ValueTask<bool> OnNextErrorAsync(Exception error, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Exception in new value for consumption {identifier}: {error.Message}");
            return false;
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
        public Task Completion => _taskCompletion.Task;

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public async ValueTask OnCompletedAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Fin modified consumption {identifier} / {Environment.CurrentManagedThreadId}");
            _taskCompletion.SetResult();
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public async ValueTask OnErrorAsync(Exception error, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Exception in modified consumption {identifier}: {error.Message}");
            _taskCompletion.SetException(error);
        }

        /// <summary>
        /// Notifies the observer that the provider has been canceled from further consumption, as an asynchronous operation.
        /// </summary>
        /// <param name="canceledToken">The cancellation token that was cancelled during consumption.</param>
        /// <returns>
        /// A <see cref="ValueTask" /> that represents the asynchronous operation.
        /// </returns>
        public async ValueTask OnCanceledAsync(CancellationToken canceledToken)
        {
            Console.WriteLine($"Modified consumption was canceled {identifier}");
            _taskCompletion.SetCanceled(canceledToken);
        }

        /// <summary>
        /// Provides the observer with new data.
        /// </summary>
        /// <param name="value">The current notification information.</param>
        public async ValueTask OnNextAsync(int value, CancellationToken cancellationToken)
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

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition while handling a next value, as an asynchronous operation.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation. <c>Result</c> should contain a boolean flag; if <c>true</c>, the error will be propagated up the chain and will be received in OnErrorAsync as well.</returns>
        public async ValueTask<bool> OnNextErrorAsync(Exception error, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Exception in new value for modified consumption {identifier}: {error.Message}");
            return false;
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

        // update to async interfaces
        List<IAsyncDisposable> consumerDisposables = [];
        AsyncObservable<int> observable = new(producer.ReadAllAsync(cancellationToken));

        for (int i = 0; i < consumers; ++i)
        {
            string name = $"Consumer {i}";
            // update to async interfaces
            IAsyncObserver<int> consumer;
            if (i % 2 == 0)
            {
                Consumer implementation = new(name, synchronize);
                tasks.Add(implementation.Completion);
                consumer = implementation;
            }
            else
            {
                Consumer2 implementation = new(name, synchronize);
                tasks.Add(implementation.Completion);
                consumer = implementation;
            }
            // update to async interfaces
            consumerDisposables.Add(await observable.SubscribeAsync(consumer, false, cancellationToken).ConfigureAwait(false));
        }

        tasks.Add(producer.Run(cancellationToken));

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
            // update to async interfaces
            foreach (IAsyncDisposable disposable in consumerDisposables)
            {
                await disposable.DisposeAsync();
            }
            synchronize.Dispose();
        }
    }
}
