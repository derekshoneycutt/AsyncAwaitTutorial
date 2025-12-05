using System.Threading.Channels;

namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates how you can turn an IAsyncEnumerable into an IObservable and utilize that to handle all events.
/// </summary>
public static class IObservableSample
{
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
        /// Produces values from 2 ranges subsequently into the given channel as an asynchronous operation.
        /// </summary>
        /// <param name="firstStart">The first range start.</param>
        /// <param name="firstEnd">The first range end.</param>
        /// <param name="secondStart">The second range start.</param>
        /// <param name="secondEnd">The second range end.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public async Task Produce(
            int firstStart, int firstEnd, int secondStart, int secondEnd,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"Producing on {Environment.CurrentManagedThreadId}...");

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

            Console.WriteLine($"Fin Prod on {Environment.CurrentManagedThreadId}");
        }

        /// <summary>
        /// Runs the specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        public async Task Run(CancellationToken cancellationToken)
        {
            List<Task> producerTasks = [];
            for (int i = 0; i < count; ++i)
            {
                int mod = i == 0 ? 0 : unchecked((int)Math.Pow(10.0, i));
                producerTasks.Add(Produce(
                    1 + mod, 5 + mod, 1000000001 + mod, 1000000005 + mod, cancellationToken));
            }

            await Task.WhenAll(producerTasks);

            _channel.Writer.Complete();
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
    /// Simpel disposable class that cancels a cancellation token upon disposal
    /// </summary>
    public class CancellationTokenDisposable : IDisposable
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
    /// Observable class that turns IAsyncEnumerable into the IObservable interface
    /// </summary>
    /// <typeparam name="T">The type of messages in the collection.</typeparam>
    public class Observable<T>(IAsyncEnumerable<T> source) : IObservable<T>
    {
        /// <summary>
        /// Called when a new value is received, as an asynchronous operation.
        /// </summary>
        /// <param name="observer">The observer to notify of new values and errors.</param>
        /// <param name="value">The value to notify.</param>
        /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
        private async Task OnNewValueAsync(
            IObserver<T> observer, T value, CancellationToken cancellationToken)
        {
            try
            {
                observer.OnNext(value);
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
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
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
            finally
            {
                observer.OnCompleted();
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
    /// Converts the IAsyncEnumerable instance to an IObservable instance.
    /// </summary>
    /// <typeparam name="T">The type of message in the collection</typeparam>
    /// <param name="source">The source to convert.</param>
    /// <returns>A new <see cref="IObservable{T}"/> that observes the asynchronous collection.</returns>
    public static IObservable<T> ToObservable<T>(this IAsyncEnumerable<T> source)
    {
        return new Observable<T>(source);
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
        public Task Task => _taskCompletion.Task;

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public void OnCompleted()
        {
            Console.WriteLine($"Fin Cons {identifier} / {Environment.CurrentManagedThreadId}");
            _taskCompletion.SetResult();
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error)
        {
            Console.WriteLine($"Exception in new value for consumer {identifier}: {error.Message}");
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
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task Run(
        CancellationToken cancellationToken)
    {
        int producers = 5;
        int consumers = 9;

        List<Consumer> consumerObservers = [];
        List<IDisposable> consumerDisposables = [];

        Producer producer = new(producers);
        Task producerHost = producer.Run(cancellationToken);

        IObservable<int> observable = producer.ReadAllValuesAsync(cancellationToken).ToObservable();

        for (int i = 0; i < consumers; ++i)
        {
            string name = $"Consumer {i}";
            Consumer consumer = new(name);
            consumerObservers.Add(consumer);
            consumerDisposables.Add(observable.Subscribe(consumer));
        }

        await Task.WhenAll([producerHost, .. consumerObservers.Select(obs => obs.Task)]).ConfigureAwait(false);

        foreach (IDisposable disposable in consumerDisposables)
        {
            disposable.Dispose();
        }
    }
}
