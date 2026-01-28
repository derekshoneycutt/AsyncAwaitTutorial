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
    public static void DoubleLoop(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        TaskCompletionSource completionSource,
        CancellationToken cancellationToken)
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
    /// The producer state machine that will produce each value in the given ranges after a delay.
    /// </summary>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    public class Producer(
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        CancellationToken cancellationToken)
        : IAsyncEnumerator<int>
    {
        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            _position = Position.Initial;
            Current = -1;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Enum indicating the state position
        /// </summary>
        private enum Position
        {
            Initial,
            FirstLoop,
            SecondLoop,
            End
        }

        /// <summary>
        /// The position that the state machine is currently in
        /// </summary>
        private Position _position = Position.Initial;

        /// <summary>
        /// Gets the current value represented by the state machine.
        /// </summary>
        public int Current { get; private set; } = -1;

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        ///   <see langword="true" /> if the enumerator was successfully advanced to the next element; <see langword="false" /> if the enumerator has passed the end of the collection.
        /// </returns>
        public async ValueTask<bool> MoveNextAsync()
        {
            async ValueTask<bool> FirstLoop()
            {
                if (Current <= firstEnd)
                {
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                    return true;
                }

                _position = Position.SecondLoop;
                Current = secondStart;
                return await SecondLoop().ConfigureAwait(false);
            }

            async ValueTask<bool> SecondLoop()
            {
                if (Current <= secondEnd)
                {
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                    return true;
                }

                _position = Position.End;
                return false;
            }

            switch (_position)
            {
                case Position.Initial:
                    Current = firstStart;
                    _position = Position.FirstLoop;
                    return await FirstLoop().ConfigureAwait(false);

                case Position.FirstLoop:
                    ++Current;
                    return await FirstLoop().ConfigureAwait(false);

                case Position.SecondLoop:
                    ++Current;
                    return await SecondLoop().ConfigureAwait(false);

                default:
                    throw new InvalidOperationException();
            }
        }
    }

    /// <summary>
    /// Enumerable implementation to complete the Iterator implementation
    /// </summary>
    public class ProductionEnumerable(int firstStart, int firstEnd, int secondStart, int secondEnd)
        : IAsyncEnumerable<int>
    {
        public IAsyncEnumerator<int> GetAsyncEnumerator(
            CancellationToken cancellationToken)
        {
            return new Producer(firstStart, firstEnd, secondStart, secondEnd, cancellationToken);
        }
    }

    /// <summary>
    /// Consumes the collection, printing each value to the screen
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The values to print to the screen.</param>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task Consume(
        string identifier,
        IAsyncEnumerable<int> values,
        CancellationToken cancellationToken)
    {
        // Update to taking an IAsyncEnumerable

        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
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
        //We can clean up the cancellation Token stuff

        List<Task> tasks = [];
        AsyncLocal<int> mod = new();
        for (int index = 1; index <= 55; ++index)
        {
            mod.Value = 10 * index;
            string identifier = $"Action {index}";
            // Update to the new IAsyncEnumerable implementation
            IAsyncEnumerable<int> values = new ProductionEnumerable(
                1 + mod.Value, 5 + mod.Value,
                1001 + mod.Value, 1005 + mod.Value);
            tasks.Add(Consume(identifier, values, cancellationToken));
        }

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        TaskCompletionSource backThreadSource = new();
        Thread instanceCaller = new(new ThreadStart(() =>
            DoubleLoop("Single Thread",
                1, 5,
                101, 105,
                backThreadSource, cancellationToken)));
        instanceCaller.Start();
        tasks.Add(backThreadSource.Task);

        await Task.WhenAll(tasks).ConfigureAwait(false);

        Console.WriteLine("All fin");
    }
}
