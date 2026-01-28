/*
 * =====================================================
 *         Step 3 : State Machine Producer Sample
 * 
 *  The point of this sample is to use a state machine to
 *  evolve our code so that values can be consumed as they
 *  are produced, instead of waiting for all values to be
 *  produced before we consume any.
 *  
 *  
 *  A.  Start by creating a Producer class that takes 2
 *      ranges in the primary constructor and has a Current
 *      property that is private set public get, and a
 *      public bool MoveNext method.
 *      
 *  B.  Add a Position enum with the possible states of
 *      the producer: Initial, FirstLoop, SecondLoop, End.
 *      Make a private property starting at Initial in Producer.
 *      
 *  C.  Fill out the MoveNext method to perform the full logic
 *      of the two loops. Include Thread.Sleep here to
 *      simulate some operation like a web server call or the like.
 *      
 *  D.  Update Consume to consume a Producer instance
 *      with a while loop. Take out the Sleep in here.
 *      
 *  E.  Update the Run code to create an instance of Producer
 *      and pass it to Consume at each iteration.
 *      
 * This takes us a step further so that we now can consume
 * values as we write them. It also gets us familiar with
 * state machines and how they work at a very basic level.
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// Use a state machine to produce values and allow simultaneous consumption instead of producing into a list
/// </summary>
public class StateMachineProducerSample : ITutorialSample
{
    /// <summary>
    /// The producer state machine that will produce each value in the given ranges after a delay.
    /// </summary>
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    public class Producer(int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
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
        /// Advances the state machine to the next element.
        /// </summary>
        /// <returns>
        ///   <see langword="true" /> if the state machine was successfully advanced to the next element; <see langword="false" /> if the machine has passed the end.
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
    /// Consumes the collection, printing each value to the screen
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="values">The values to print to the screen.</param>
    public static void Consume(
        string identifier,
        Producer producer)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        while (producer.MoveNext())
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {producer.Current}");
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
            Producer producer = new(
                1 + mod, 5 + mod,
                1001 + mod, 1005 + mod);
            Consume(identifier, producer);
        }

        Console.WriteLine("All fin");
    }
}
