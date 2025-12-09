/*
 * =====================================================
 *         Step 12 : A basic state machine
 * 
 *  This is a major tangent from prior code to demonstrate the
 *  basic concepts of state machines and iterators. We basically take
 *  Step 1 -- Procedural Sample -- and refactor it into a C-style
 *  state machine. We run though a single instance of the state
 *  process in the Run method. This will be a foundation for
 *  understanding how our previous async code becomes async/await.
 *  
 *  
 *  A.  Copy Step 1. We will start with an entirely synchronous version again.
 *  
 *  B.  Create the MyState class, with associated StatePosition enum.
 *      This will just be a basic POCO, simulating an old
 *      C style struct used for this kind of state machine
 *      in old days!
 *      
 *  C.  Create a new MoveNext() method that walks through each call,
 *      producing the next value in the state. This state
 *      machine will only be a producer, producing integer
 *      values for us to print on screen.
 *      
 *  D.  Update Run and InstanceMethod to use the new state machine
 *      to produce the values to print.
 *      
 * This is a new, big step, taking a step back from async
 * but getting us familiar with what is happening with
 * some of the new language features we utilize.
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// This sample demonstrates creating a basic state machine
/// </summary>
public class StateMachineSample : ITutorialSample
{

    /// <summary>
    /// State class managed by the state machine
    /// </summary>
    public record MyState(
        string Identifier,
        int FirstStart, int FirstEnd, int SecondStart, int SecondEnd)
    {
        /// <summary>
        /// Enum describing the current position of the state machine
        /// </summary>
        private enum StatePosition
        {
            Initial,

            FirstLoop,

            SecondLoop,

            End
        }

        /// <summary>
        /// The current position of the operation.
        /// </summary>
        private StatePosition _position = StatePosition.Initial;

        /// <summary>
        /// The current value representing the end value the state machine is reaching for in the current "loop"
        /// </summary>
        private int _currentEnd  = -1;

        /// <summary>
        /// Gets or sets the current value represented by the state.
        /// </summary>
        public int Current { get; set; } = -1;

        /// <summary>
        /// Moves to the next position in the state machine.
        /// </summary>
        /// <param name="state">The state to advance to the next position.</param>
        /// <returns>Whether or not the state machine currently holds a valid state</returns>
        public bool MoveNext()
        {
            bool FirstLoop()
            {
                if (Current <= _currentEnd)
                {
                    Thread.Sleep(500);
                    return true;
                }

                _position = StatePosition.SecondLoop;
                (Current, _currentEnd) = SecondStart <= SecondEnd ? (SecondStart, SecondEnd) : (SecondEnd, SecondStart);
                Current = SecondStart;
                return SecondLoop();
            }

            bool SecondLoop()
            {
                if (Current <= _currentEnd)
                {
                    Thread.Sleep(500);
                    return true;
                }

                _position = StatePosition.End;

                Console.WriteLine($"Fin producer {Identifier} / {Environment.CurrentManagedThreadId}");
                return false;
            }

            switch (_position)
            {
                case StatePosition.Initial:
                    Console.WriteLine($"Writing producer: {Identifier} / {Environment.CurrentManagedThreadId}");

                    (Current, _currentEnd) = FirstStart <= FirstEnd ? (FirstStart, FirstEnd) : (FirstEnd, FirstStart);
                    _position = StatePosition.FirstLoop;
                    return FirstLoop();

                case StatePosition.FirstLoop:
                    ++Current;
                    return FirstLoop();

                case StatePosition.SecondLoop:
                    ++Current;
                    return SecondLoop();

                default:
                    throw new InvalidOperationException("Cannot continue on a finished state machine.");
            }
        }
    }

    /// <summary>
    /// The instance method to run as independent examples in the sample. This is a synchronous method.
    /// </summary>
    /// <param name="identifier">The identifier to print as the name of the current instance.</param>
    /// <param name="myState">The state to loop through and display values from.</param>
    public static void InstanceMethod(
        string identifier,
        MyState myState)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        while (myState.MoveNext())
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {myState.Current}");
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
            string identifier = $"Action {i + 1}";
            // Create and pass the new state object here
            MyState myState = new(identifier,
                1 + mod, 5 + mod,
                1001 + mod, 1005 + mod);
            InstanceMethod(identifier, myState);
        }

        Console.WriteLine("All fin");
    }
}
