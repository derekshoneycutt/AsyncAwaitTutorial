using System.Collections;

namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates creating a basic IEnumerable implementation from the ground up, with 2 inner loops.
/// </summary>
/// <remarks>
/// This is basically just adopting our previous state machine into a formal IEnumerable structure.
/// </remarks>
public class IEnumerableSample : ITutorialSample
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
    /// The enumerator that moves to the next position with each MoveNext
    /// </summary>
    /// <seealso cref="IEnumerator{Int32}" />
    public class MyEnumerator(int firstStart, int firstEnd, int secondStart, int secondEnd)
        : IEnumerator<int>
    {
        /// <summary>
        /// The current position of the state we are iterating over
        /// </summary>
        private StatePosition _position = StatePosition.Initial;

        /// <summary>
        /// The current value of the state that we are iterating over
        /// </summary>
        private int _currentValue = -1;

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        object IEnumerator.Current => _currentValue;

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public int Current => _currentValue;

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        ///   <see langword="true" /> if the enumerator was successfully advanced to the next element; <see langword="false" /> if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="InvalidOperationException">Cannot continue on a finished state machine.</exception>
        public bool MoveNext()
        {
            // We move our MoveNextMyState into the MoveNext method of the enumerator with minor updates
            switch (_position)
            {
                case StatePosition.Initial:
                    Thread.Sleep(500);
                    _position = StatePosition.FirstLoop;
                    _currentValue = firstStart;
                    return true;

                case StatePosition.FirstLoop:
                    Thread.Sleep(500);
                    ++_currentValue;
                    if (_currentValue > firstEnd)
                    {
                        _currentValue = secondStart;
                        _position = StatePosition.SecondLoop;
                    }
                    return true;

                case StatePosition.SecondLoop:
                    Thread.Sleep(500);
                    ++_currentValue;
                    if (_currentValue > secondEnd)
                    {
                        _position = StatePosition.End;
                        return false;
                    }
                    return true;

                default:
                    throw new InvalidOperationException("Cannot continue on a finished state machine.");
            }
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            _position = StatePosition.Initial;
            _currentValue = 0;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Enumerable that loops over 2 ranges subsequently
    /// </summary>
    /// <seealso cref="IEnumerable{int}" />
    public class MyEnumerable(int firstStart, int firstEnd, int secondStart, int secondEnd)
        : IEnumerable<int>
    {


        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<int> GetEnumerator()
        {
            return new MyEnumerator(firstStart, firstEnd, secondStart, secondEnd);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// The instance method to run as independent examples in the sample. This is a synchronous method.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    public static void InstanceMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        MyEnumerable myState = new(1, 5, 101, 105);

        //List<int> listed = [.. myState]; // Note the long delay that is the multiple Thread.Sleep occurring in this call!

        foreach (int value in myState)
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");
    }



    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(CancellationToken cancellationToken)
    {
        int actionCount = 5;
        for (int i = 0; i < actionCount; ++i)
        {
            int mod = 10 * i;
            string action = $"Action {i}";
            InstanceMethod(action, 1 + mod, 5 + mod, 1001 + mod, 1005 + mod);
        }

        Console.WriteLine("All fin");
    }
}
