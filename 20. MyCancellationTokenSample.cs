namespace AsyncAwaitTutorial;

/// <summary>
/// Sample used to demonstrate the structure of cancellation tokens by creating a custom cancellation token type
/// </summary>
public static class MyCancellationTokenSample
{
    /// <summary>
    /// Struct representing a cancellation token to notify the requested cancellation of an operation
    /// </summary>
    public readonly struct MyCancellationToken
    {
        /// <summary>
        /// The cancellation source that this token wraps
        /// </summary>
        private readonly MyCancellationTokenSource _source;

        /// <summary>
        /// Initializes a new instance of the <see cref="MyCancellationToken"/> struct.
        /// </summary>
        /// <param name="source">The cancellation source to wrap.</param>
        internal MyCancellationToken(MyCancellationTokenSource source)
        {
            _source = source;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is cancellation requested.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is cancellation requested; otherwise, <c>false</c>.
        /// </value>
        public bool IsCancellationRequested => _source.IsCancellationRequested;

        /// <summary>
        /// Registers the specified callback action to perform upon cancellation.
        /// </summary>
        /// <param name="callback">The callback to perform upon cancellation.</param>
        public void Register(Action callback) => _source.Register(callback);

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
        public void Register(Action callback)
        {
            lock(_callbacks)
            {
                if (!_isCancellationRequested)
                {
                    _callbacks.Add(callback);
                    return;
                }
            }

            callback();
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
    /// <param name="firstMax">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondMax">The second maximum value, completing the second range.</param>
    /// <param name="completionSource">The Task Completion Source to mark when this task has completed</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static void InstanceMethod(
        string identifier,
        int firstStart, int firstMax, int secondStart, int secondMax,
        TaskCompletionSource completionSource,
        MyCancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

            for (int i = firstStart; i <= firstMax; i++)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
            }
            for (int i = secondStart; i <= secondMax; i++)
            {
                Thread.Sleep(1000);
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");

            completionSource.SetResult();
        }
        catch (Exception ex)
        {
            completionSource.SetException(ex);
        }
    }


    /// <summary>
    /// Loops over 2 ranges of integers subsequently as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first range start.</param>
    /// <param name="firstMax">The first range maximum.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondMax">The second range maximum.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task DoubleLoop(
        string identifier,
        int firstStart, int firstMax, int secondStart, int secondMax,
        MyCancellationToken cancellationToken)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        for (int i = firstStart; i <= firstMax; i++)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
        }
        for (int i = secondStart; i <= secondMax; i++)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {i}");
        }

        Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");
    }


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    public static async Task Run()
    {
        MyCancellationTokenSource cts = new();

        cts.Register(() =>
        {
            Console.WriteLine("Registered cancellation.");
        });

        int threadCount = 55;
        List<Task> tasks = [];
        for (int i = 0; i < threadCount; ++i)
        {
            int mod = 10 * i;
            string action = $"Action {i}";
            tasks.Add(
                DoubleLoop(action, 1 + mod, 5 + mod, 10001 + mod, 10005 + mod, cts.Token));
        }

        await Task.Delay(1500).ConfigureAwait(false);

        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            InstanceMethod("Single Thread", 1, 5, 101, 105, backThreadSource, cts.Token)));
        instanceCaller.Start();
        tasks.Add(backThreadSource.Task);

        try
        {
            await Task.Delay(3000).ConfigureAwait(false);
            cts.Cancel();

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            Console.WriteLine("Canceled");
        }
    }
}
