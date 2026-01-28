/*
 * =====================================================
 *         Step 4 : IEnumerable Producer Sample
 * 
 *  The point of this sample is to 
 *  
 *  
 *  A.  Add the IEnumerator<int> interface to the Producer class
 *      and fill in the missing parts to fulfill the full thing.
 *      
 *  B.  Create a new ProductionEnumerable that implements
 *      IEnumerable<int>, returning a Producer in GetEnumerator.
 *      
 *  C.  Make sure Consume takes an IEnumerable<int> and just use
 *      a foreach loop on it again.
 *      
 *  D.  Update the run code to create a ProductionEnumerable.
 *      
 * This now has us using the standard Iterator pattern and
 * interfaces. This is an important step to highlight how
 * additional features we will be utilizing work.
 * 
 * =====================================================
*/

using System.Collections;

namespace AsyncAwaitTutorial;

/// <summary>
/// Use the standard IEnumerable pattern to produce values and allow simultaneous consumption instead of producing into a list
/// </summary>
public class IEnumerableProducerSample : ITutorialSample
{
    /// <summary>
    /// The producer state machine that will produce each value in the given ranges after a delay.
    /// </summary>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    public class Producer(int firstStart, int firstEnd, int secondStart, int secondEnd)
        : IEnumerator<int>
    {
        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        object IEnumerator.Current => Current;

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
        public void Dispose()
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
        public bool MoveNext()
        {
            bool FirstLoop()
            {
                if (Current <= firstEnd)
                {
                    Thread.Sleep(500);
                    return true;
                }

                _position = Position.SecondLoop;
                Current = secondStart;
                return SecondLoop();
            }

            bool SecondLoop()
            {
                if (Current <= secondEnd)
                {
                    Thread.Sleep(500);
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
                    return FirstLoop();

                case Position.FirstLoop:
                    ++Current;
                    return FirstLoop();

                case Position.SecondLoop:
                    ++Current;
                    return SecondLoop();

                default:
                    throw new InvalidOperationException();
            }
        }
    }

    /// <summary>
    /// Enumerable implementation to complete the Iterator implementation
    /// </summary>
    public class ProductionEnumerable(int firstStart, int firstEnd, int secondStart, int secondEnd)
        : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            return new Producer(firstStart, firstEnd, secondStart, secondEnd);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Consumes the collection, printing each value to the screen
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The values to print to the screen.</param>
    public static void Consume(
        string identifier,
        IEnumerable<int> values)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        foreach (int value in values)
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(CancellationToken cancellationToken)
    {
        for (int index = 1; index <= 5; ++index)
        {
            int mod = 10 * index;
            string identifier = $"Action {index}";
            ProductionEnumerable producer = new(
                1 + mod, 5 + mod,
                1001 + mod, 1005 + mod);
            Consume(identifier, producer);
        }

        Console.WriteLine("All fin");
    }
}
