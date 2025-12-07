namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates construction of an IAsyncEnumerable as a custom implementation
/// </summary>
/// <remarks>
/// This take the previous sample and updates it with a custom implementation of IAsyncEnumerable and IAsyncEnumerator.
/// Due to the decoupling of the 2 loops in a previous step, we just use a single loop in the enumerator state machine
/// this time.
/// </remarks>
public class CustomAsyncEnumerableSample : ITutorialSample
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
    /// Enum describing the current position of the state being iterated over
    /// </summary>
    public enum StatePosition
    {
        Initial,

        InLoop,

        End
    }

    /// <summary>
    /// Custom enumerator that moves along each value in the 2 ranges, pausing to delay asynchronously each instance
    /// </summary>
    /// <seealso cref="IAsyncEnumerator{Int32}" />
    public class MyAsyncEnumerator(int start, int end, CancellationToken cancellationToken)
        : IAsyncEnumerator<int>
    {
        /// <summary>
        /// The current position that the enumerator is in
        /// </summary>
        private StatePosition _position = StatePosition.Initial;

        /// <summary>
        /// The current value that the enumerator will return
        /// </summary>
        private int _currentValue = 0;

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public int Current => _currentValue;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous dispose operation.
        /// </returns>
        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }


        /// <summary>
        /// Advances the enumerator asynchronously to the next element of the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="ValueTask{Boolean}" /> that will complete with a result of <c>true</c> if the enumerator
        /// was successfully advanced to the next element, or <c>false</c> if the enumerator has passed the end
        /// of the collection.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Cannot continue on a finished state machine.</exception>
        public async ValueTask<bool> MoveNextAsync()
        {
            switch (_position)
            {
                case StatePosition.Initial:
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                    _position = StatePosition.InLoop;
                    _currentValue = start;
                    return true;

                case StatePosition.InLoop:
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                    ++_currentValue;
                    if (_currentValue > end)
                    {
                        _position = StatePosition.End;
                        return false;
                    }
                    return true;

                default:
                    throw new InvalidOperationException("Cannot continue on a finished state machine.");
            }
        }
    }


    /// <summary>
    /// Custom enumerable that will asynchronously iterate over 2 ranges with delays for each iteration
    /// </summary>
    /// <seealso cref="IAsyncEnumerable{Int32}" />
    public class MyAsyncEnumerable(int start, int end)
        : IAsyncEnumerable<int>
    {
        public IAsyncEnumerator<int> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            return new MyAsyncEnumerator(start, end, cancellationToken);
        }
    }


    // We don't need the dual method thing any more, as we have sent the entire production of values off into the MyAsyncEnumerable functionality

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
    /// Loops over the first of integers subsequently as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first range start.</param>
    /// <param name="firstEnd">The first range maximum.</param>
    /// <param name="secondLoopSignal">The semaphore to signal when the first loop is complete.</param>
    /// <param name="synchronize">The semaphore used to synchronize printing values to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task FirstLoop(
        string identifier,
        int firstStart, int firstEnd,
        SemaphoreSlim secondLoopSignal,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Writing first values: {identifier} / {Environment.CurrentManagedThreadId}");

        // Just get a new MyAsyncEnumerable now instead
        // await foreach on this gives us the value directly!
        MyAsyncEnumerable numbers = new(firstStart, firstEnd);
        await foreach (int value in numbers.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await OnNewValue(identifier, value, synchronize, cancellationToken).ConfigureAwait(false);
        }

        secondLoopSignal.Release();

        Console.WriteLine($"Fin first {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Loops over the second range of integers subsequently as an asynchronous operation
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="secondStart">The second range start.</param>
    /// <param name="secondEnd">The second range maximum.</param>
    /// <param name="secondLoopSignal">The semaphore that signals when the first loop is complete.</param>
    /// <param name="synchronize">The semaphore used to synchronize printing values to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task SecondLoop(
        string identifier,
        int secondStart, int secondEnd,
        SemaphoreSlim secondLoopSignal,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Writing second values: {identifier} / {Environment.CurrentManagedThreadId}");

        await secondLoopSignal.WaitAsync(cancellationToken).ConfigureAwait(false);

        // Just get a new MyAsyncEnumerable now instead
        // await foreach on this gives us the value directly!
        MyAsyncEnumerable numbers = new(secondStart, secondEnd);
        await foreach (int value in numbers.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await OnNewValue(identifier, value, synchronize, cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"Fin second {identifier} / {Environment.CurrentManagedThreadId}");
    }


    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        int actionCount = 55;
        List<Task> tasks = [];
        SemaphoreSlim synchronize = new(1);
        List<SemaphoreSlim> semaphores = [synchronize];
        AsyncLocal<int> mod = new();
        for (int i = 0; i < actionCount; ++i)
        {
            mod.Value = 10 * i;
            string action = $"Action {i}";
            SemaphoreSlim secondLoopSignal = new(0);
            semaphores.Add(secondLoopSignal);
            tasks.Add(
                FirstLoop(action, 1 + mod.Value, 5 + mod.Value, secondLoopSignal, synchronize, cancellationToken));
            tasks.Add(
                SecondLoop(action, 10001 + mod.Value, 10005 + mod.Value, secondLoopSignal, synchronize, cancellationToken));
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
