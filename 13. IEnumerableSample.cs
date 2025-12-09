/*
 * =====================================================
 *         Step 13 : Implementing the state machine as IEnumerable
 * 
 *  Taking from our state machine, we want to build it into the
 *  IEnumerable iterators that we can use in C# for our
 *  demonstration purposes.
 *  
 *  
 *  A.  Copy Step 12. We will update this code.
 *  
 *  B.  Create initial IEnumerable class implementation.
 *  
 *  C.  Create the IEnumerator class implementation and link
 *      it directly to the IEnumerable class's GetEnumerator
 *      method with new. We can just move the MoveNext method
 *      from our state machine directly into the Enumerator
 *      MoveNext method, fixing references to use the local
 *      values. It should be easy to implement everything.
 *      
 *  D.  Update Run and InstanceMethod to create one of these
 *      new Enumerable objects and iterate on it with foreach.
 *      You can move them into a class, etc. to see that
 *      the delayed execution continues.
 *      
 * This is a pretty simple step, just transitioning to
 * IEnumerable instead of an entirely manual state machine
 * process.
 * 
 * =====================================================
*/

using System.Collections;

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates creating a basic IEnumerable implementation from the ground up, with 2 inner loops.
/// </summary>
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
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public int Current { get; private set; } = -1;

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
                    Current = firstStart;
                    return true;

                case StatePosition.FirstLoop:
                    Thread.Sleep(500);
                    ++Current;
                    if (Current > firstEnd)
                    {
                        Current = secondStart;
                        _position = StatePosition.SecondLoop;
                    }
                    return true;

                case StatePosition.SecondLoop:
                    Thread.Sleep(500);
                    ++Current;
                    if (Current > secondEnd)
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
            Current = 0;
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
            (int startFirst, int endFirst) = firstStart <= firstEnd ? (firstStart, firstEnd) : (firstEnd, firstStart);
            (int startSecond, int endSecond) = secondStart <= secondEnd ? (secondStart, secondEnd) : (secondEnd, secondStart);
            return new MyEnumerator(startFirst, endFirst, startSecond, endSecond);
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
    /// <param name="values">The collection of values to print</param>
    public static void InstanceMethod(
        string identifier,
        IEnumerable<int> values)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        //List<int> listed = [.. values]; // Note the long delay that is the multiple Thread.Sleep occurring in this call!

        foreach (int value in values)
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
            string identifier = $"Action {i}";
            // Create and pass the new state object here
            MyEnumerable values = new(
                1 + mod, 5 + mod,
                1001 + mod, 1005 + mod);
            InstanceMethod(identifier, values);
        }

        Console.WriteLine("All fin");
    }
}
