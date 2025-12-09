/*
 * =====================================================
 *         Step 24 : First IAsyncEnumerable
 * 
 *  The previous sample gave us some motivations for the
 *  IAsyncEnumerable, and so now we're actually going to make
 *  one and utilize it effectively. The Concat method we use
 *  for IAsyncEnumerable is new in .net10 (previously part of
 *  Reactive Extensions), but it still has the same problem.
 *  Nonetheless, we will be able to use await foreach now
 *  and see how IAsyncEnumerable is constructed under the
 *  hood.
 *  
 *  A.  Copy Step 23. We will update this code.
 *  
 *  B.  Similar to Step 12 and 13, we want to break down our
 *      existing for loops into state machines that can be
 *      expressed in the IAsyncEnumerable/IAsyncEnumerator
 *      structure. In this, we create the implementation for
 *      each.
 *      
 *  C.  Update the Consumer method to take and consume
 *      IAsyncEnumerable<int> instead of the IEnumerable<Task<int>>.
 *      This code is looking nicer.
 *      
 *  D.  Update Run as necessary as well. This should be minimal
 *      if the implementation was created well.
 *      
 *      
 *  Creating the state machine implementation of these methods
 *  always looks like quite a lot to tackle, but it gives us
 *  a good handle on how the compiler will treat the code we
 *  produce in later steps. We do still have the same issue
 *  that Concat is not actually running our producers at the
 *  same time, however.
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates construction of an IAsyncEnumerable as a custom implementation
/// </summary>
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
    public class MyAsyncEnumerator(
        string identifier,
        int start, int end,
        SemaphoreSlim signalSecondLoop,
        bool runFirstLoop,
        CancellationToken cancellationToken)
        : IAsyncEnumerator<int>
    {
        /// <summary>
        /// The current position that the enumerator is in
        /// </summary>
        private StatePosition _position = StatePosition.Initial;

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public int Current { get; private set; } = -1;

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
        public async ValueTask<bool> MoveNextAsync()
        {
            async Task<bool> Loop()
            {
                if (Current <= end)
                {
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                    _position = StatePosition.InLoop;
                    return true;
                }

                _position = StatePosition.End;
                if (runFirstLoop)
                {
                    signalSecondLoop.Release();
                }

                Console.WriteLine($"Fin producer {identifier} / {Environment.CurrentManagedThreadId}");
                return false;
            }

            switch (_position)
            {
                case StatePosition.Initial:
                    Console.WriteLine($"Writing producer: {identifier} / {Environment.CurrentManagedThreadId}");

                    if (!runFirstLoop)
                    {
                        await signalSecondLoop.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }
                    Current = start;
                    return await Loop().ConfigureAwait(false);

                case StatePosition.InLoop:
                    ++Current;
                    return await Loop().ConfigureAwait(false);

                default:
                    throw new InvalidOperationException("Cannot continue on a finished state machine.");
            }
        }
    }

    /// <summary>
    /// Custom enumerable that will asynchronously iterate over 2 ranges with delays for each iteration
    /// </summary>
    /// <seealso cref="IAsyncEnumerable{Int32}" />
    public class MyAsyncEnumerable(
        string identifier,
        int start, int end,
        SemaphoreSlim signalSecondLoop,
        bool runFirstLoop)
        : IAsyncEnumerable<int>
    {
        public IAsyncEnumerator<int> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            (int doStart, int doEnd) = start <= end ? (start, end) : (start, end);
            return new MyAsyncEnumerator(identifier, doStart, doEnd, signalSecondLoop, runFirstLoop, cancellationToken);
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
    private static async Task OnNewValueAsync(
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
    /// <param name="values">The values stream to consume.</param>
    /// <param name="synchronize">The semaphore used to synchronize printing values to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    public static async Task Consumer(
        string identifier,
        IAsyncEnumerable<int> values,
        SemaphoreSlim synchronize,
        CancellationToken cancellationToken)
    {
        // We update Consumer to handle IAsyncEnumerable now

        Console.WriteLine($"Writing consumer: {identifier} / {Environment.CurrentManagedThreadId}");

        await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await OnNewValueAsync(identifier, value, synchronize, cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"Fin consumer {identifier} / {Environment.CurrentManagedThreadId}");
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
            string identifier = $"Action {i}";
            SemaphoreSlim secondLoopSignal = new(0);
            semaphores.Add(secondLoopSignal);
            // We update to IAsyncEnumerable
            IAsyncEnumerable<int> values =
                new MyAsyncEnumerable(identifier,
                    1 + mod.Value, 5 + mod.Value,
                    secondLoopSignal, true)
                    .Concat(new MyAsyncEnumerable(identifier,
                        1001 + mod.Value, 1005 + mod.Value,
                        secondLoopSignal, false));
            tasks.Add(
                Consumer(identifier, values, synchronize, cancellationToken));
        }

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
            foreach (SemaphoreSlim semaphore in semaphores)
            {
                semaphore.Dispose();
            }
        }
    }
}
