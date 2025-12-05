using System.Collections;

namespace AsyncAwaitTutorial;


/// <summary>
/// This sample demonstrates creating a basic IEnumerable implementation from the ground up, with 2 inner loops
/// </summary>
public static class IEnumerableSample
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
    public class MyEnumerator(int firstStart, int firstMax, int secondStart, int secondMax)
        : IEnumerator<int>
    {
        /// <summary>
        /// The current position of the state we are iterating over
        /// </summary>
        private StatePosition _position = StatePosition.Initial;

        /// <summary>
        /// The current value of the state that we are iterating over
        /// </summary>
        private int _currentValue = 0;

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        ///   <see langword="true" /> if the enumerator was successfully advanced to the next element; <see langword="false" /> if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="InvalidOperationException">Cannot continue on a finished state machine.</exception>
        public bool MoveNext()
        {
            switch (_position)
            {
                case StatePosition.Initial:
                    Thread.Sleep(1000);
                    _position = StatePosition.FirstLoop;
                    _currentValue = firstStart;
                    return true;

                case StatePosition.FirstLoop:
                    Thread.Sleep(1000);
                    ++_currentValue;
                    if (_currentValue > firstMax)
                    {
                        _currentValue = secondStart;
                        _position = StatePosition.SecondLoop;
                    }
                    return true;

                case StatePosition.SecondLoop:
                    Thread.Sleep(1000);
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

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            _position = StatePosition.Initial;
            _currentValue = 0;
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        object IEnumerator.Current
        {
            get
            {
                return _currentValue;
            }
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public int Current
        {
            get
            {
                return _currentValue;
            }
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
    public class MyEnumerable(int firstStart, int firstMax, int secondStart, int secondMax)
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
            return new MyEnumerator(firstStart, firstMax, secondStart, secondMax);
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
    /// Runs sample code for the sample.
    /// </summary>
    public static void Run()
    {
        MyEnumerable myState = new(1, 5, 101, 105);

        //List<int> listed = [.. myState]; // Note the long delay that is the multiple Thread.Sleep occurring in this call!

        foreach (int value in myState)
        {
            Console.WriteLine(value);
        }
    }
}
