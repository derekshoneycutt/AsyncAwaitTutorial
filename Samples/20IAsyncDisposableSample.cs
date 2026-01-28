namespace AsyncAwaitTutorial;

/// <summary>
/// Sample adding a CancellationTokenRegistration in order to demonstrate IAsyncEnumerable
/// </summary>
public class IAsyncDisposableSample : ITutorialSample
{
    /// <summary>
    /// Struct representing a cancellation token to notify the requested cancellation of an operation
    /// </summary>
    public readonly struct MyCancellationToken(MyCancellationTokenSource source)
    {
        /// <summary>
        /// Gets a value indicating whether this instance is cancellation requested.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is cancellation requested; otherwise, <c>false</c>.
        /// </value>
        public bool IsCancellationRequested => source.IsCancellationRequested;

        /// <summary>
        /// Registers the specified callback action to perform upon cancellation.
        /// </summary>
        /// <param name="callback">The callback to perform upon cancellation.</param>
        public void Register(Action callback) => source.Register(callback);

        /// <summary>
        /// Throws if a cancellation has been requested.
        /// </summary>
        /// <exception cref="OperationCanceledException"></exception>
        public void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
        }
    }

    /// <summary>
    /// A registration handle that unregisters the callback when disposed.
    /// </summary>
    public readonly struct MyCancellationTokenRegistration(
        MyCancellationTokenSource source, Action callback)
        : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            source.Unregister(callback);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources as an asynchronous operation.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            source.Unregister(callback);
        }
    }

    /// <summary>
    /// The source used to get a cancellation token and request cancellation with it
    /// </summary>
    public class MyCancellationTokenSource
    {
        /// <summary>
        /// Flag indicating if this cancellation has been requested
        /// </summary>
        private volatile bool _isCancellationRequested = false;

        /// <summary>
        /// The callbacks to call upon cancellation
        /// </summary>
        private readonly List<Action> _callbacks = [];

        /// <summary>
        /// The token to share with operations for cancellation
        /// </summary>
        private readonly MyCancellationToken _token;

        /// <summary>
        /// Gets the token to share with operations that may need to be cancelled.
        /// </summary>
        public MyCancellationToken Token => _token;

        /// <summary>
        /// Gets a value indicating whether this instance is cancellation requested.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is cancellation requested; otherwise, <c>false</c>.
        /// </value>
        public bool IsCancellationRequested => _isCancellationRequested;

        /// <summary>
        /// Initializes a new instance of the <see cref="MyCancellationTokenSource"/> class.
        /// </summary>
        public MyCancellationTokenSource()
        {
            _token = new(this);
        }

        /// <summary>
        /// Registers the specified callback action to perform upon cancellation.
        /// </summary>
        /// <param name="callback">The callback to perform upon cancellation.</param>
        public MyCancellationTokenRegistration Register(Action callback)
        {
            lock (_callbacks)
            {
                if (!_isCancellationRequested)
                {
                    _callbacks.Add(callback);
                    return new(this, callback);
                }
            }

            callback();
            return new(this, callback);
        }

        /// <summary>
        /// Unregisters the specified callback.
        /// </summary>
        public void Unregister(Action callback)
        {
            lock (_callbacks)
            {
                if (!_isCancellationRequested && _callbacks.Contains(callback))
                {
                    _callbacks.Remove(callback);
                }
            }
        }

        /// <summary>
        /// Cancels this instance, notifying all registered callbacks and polling methods.
        /// </summary>
        public void Cancel()
        {
            lock (_callbacks)
            {
                if (_isCancellationRequested)
                {
                    return;
                }

                _isCancellationRequested = true;
            }

            foreach (Action callback in _callbacks)
            {
                callback();
            }
        }
    }

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
        MyCancellationToken cancellationToken)
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

            Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");

            completionSource.SetResult();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            completionSource.SetCanceled();
        }
        catch (Exception ex)
        {
            completionSource.SetException(ex);
        }
    }

    /// <summary>
    /// Delays for a second and then returns a given number as an asynchronous operation.
    /// </summary>
    /// <param name="number">The number to return.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A <see cref="Task{Int32}"/> that represents the asynchronous operation. <c>Result</c> contains the specified integer.</returns>
    public static async Task<int> DelayOnNumber(
        int number,
        MyCancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(1000).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return number;
    }

    /// <summary>
    /// Produces the specified ranges of values.
    /// </summary>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A list of the produced values</returns>
    public static IEnumerable<Task<int>> Produce(
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        MyCancellationToken cancellationToken)
    {
        for (int value = firstStart; value <= firstEnd; ++value)
        {
            yield return DelayOnNumber(value, cancellationToken);
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            yield return DelayOnNumber(value, cancellationToken);
        }
    }

    /// <summary>
    /// Consumes the collection, printing each value to the screen
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The values to print to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that completes when the asynchronous operation has finished.</returns>
    public static async Task Consume(
        string identifier,
        IEnumerable<Task<int>> values,
        MyCancellationToken cancellationToken)
    {
        // We add a cancellation token parameter and add a poll to the cancellation token to ensure that we end if the process is continuing

        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        foreach (Task<int> valueTask in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int value = await valueTask.ConfigureAwait(false);
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
        MyCancellationTokenSource cts = new();

        // We can use await using here to ensure it is unregistered cleanly,
        // or we can manually dispose later
        await using MyCancellationTokenRegistration cancelRegister = cts.Register(() =>
        {
            Console.WriteLine("Registered cancellation.");
        });

        List<Task> tasks = [];
        AsyncLocal<int> mod = new();
        for (int index = 1; index <= 55; ++index)
        {
            mod.Value = 10 * index;
            string identifier = $"Action {index}";
            IEnumerable<Task<int>> values = Produce(
                1 + mod.Value, 5 + mod.Value,
                1001 + mod.Value, 1005 + mod.Value,
                cts.Token);
            tasks.Add(Consume(identifier, values, cts.Token));
        }

        await Task.Delay(500).ConfigureAwait(false);
        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            DoubleLoop("Single Thread",
                1, 5,
                101, 105,
                backThreadSource, cts.Token)));
        instanceCaller.Start();
        tasks.Add(backThreadSource.Task);

        try
        {
            await Task.Delay(3000).ConfigureAwait(false);
            // We can unregister the callback prior to cancellation, or let it be called
            //await cancelRegister.DisposeAsync().ConfigureAwait(false);
            cts.Cancel();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            Console.WriteLine("All fin");
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            Console.WriteLine("Canceled");
        }
    }
}
