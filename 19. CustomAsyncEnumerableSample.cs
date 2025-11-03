namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates construction of an IAsyncEnumerable as a custom implementation
/// </summary>
public static class CustomAsyncEnumerableSample
{
    /// <summary>
    /// Enum describing the current position of the state being iterated over
    /// </summary>
    public enum StatePosition
    {
        Initial,

        FirstLoop,

        SecondLoop,

        End
    }

    /// <summary>
    /// Custom enumerator that moves along each value in the 2 ranges, pausing to delay asynchronously each instance
    /// </summary>
    /// <seealso cref="IAsyncEnumerator{Int32}" />
    public class MyAsyncEnumerator(int firstStart, int firstMax, int secondStart, int secondMax)
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
                    await Task.Delay(1000).ConfigureAwait(false);
                    _position = StatePosition.FirstLoop;
                    _currentValue = firstStart;
                    return true;

                case StatePosition.FirstLoop:
                    await Task.Delay(1000).ConfigureAwait(false);
                    ++_currentValue;
                    if (_currentValue > firstMax)
                    {
                        _currentValue = secondStart;
                        _position = StatePosition.SecondLoop;
                    }
                    return true;

                case StatePosition.SecondLoop:
                    await Task.Delay(1000).ConfigureAwait(false);
                    ++_currentValue;
                    if (_currentValue > secondMax)
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
    public class MyAsyncEnumerable(int firstStart, int firstMax, int secondStart, int secondMax)
        : IAsyncEnumerable<int>
    {
        public IAsyncEnumerator<int> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            return new MyAsyncEnumerator(firstStart, firstMax, secondStart, secondMax);
        }
    }



    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public static async Task Run(CancellationToken cancellationToken)
    {
        MyAsyncEnumerable myState = new(1, 5, 101, 105);

        await foreach (int value in myState)
        {
            Console.WriteLine(value);
        }
    }
}
