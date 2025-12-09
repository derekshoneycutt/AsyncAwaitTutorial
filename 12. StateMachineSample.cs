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
    /// Enum describing the current position of the state machine
    /// </summary>
    public enum StatePosition
    {
        Initial,

        FirstLoop,

        SecondLoop,

        End
    }

    /// <summary>
    /// State class managed by the state machine
    /// </summary>
    public class MyState(int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        /// <summary>
        /// Gets or sets the current position of the operation.
        /// </summary>
        public StatePosition Position { get; set; } = StatePosition.Initial;

        /// <summary>
        /// Gets or sets the current value represented by the state.
        /// </summary>
        public int Current { get; set; } = -1;

        /// <summary>
        /// Gets the start of the first range to loop through.
        /// </summary>
        public int FirstStart { get; init; } = firstStart <= firstEnd ? firstStart : firstEnd;

        /// <summary>
        /// Gets the max of the first range to loop through.
        /// </summary>
        public int FirstEnd { get; init; } = firstStart <= firstEnd ? firstEnd : firstStart;

        /// <summary>
        /// Gets the start of the second range to loop through.
        /// </summary>
        public int SecondStart { get; init; } = secondStart <= secondEnd ? secondStart : secondEnd;

        /// <summary>
        /// Gets the max of the second range to loop through.
        /// </summary>
        public int SecondEnd { get; init; } = secondStart <= secondEnd ? secondEnd : secondStart;
    }


    /// <summary>
    /// Moves to the next position in the state machine.
    /// </summary>
    /// <param name="state">The state to advance to the next position.</param>
    /// <returns>The current value of the state machine</returns>
    /// <exception cref="InvalidOperationException">Cannot continue on a finished state machine.</exception>
    public static bool MoveNext(MyState state)
    {
        switch (state.Position)
        {
            case StatePosition.Initial:
                Thread.Sleep(500);
                state.Position = StatePosition.FirstLoop;
                state.Current = state.FirstStart;
                return true;

            case StatePosition.FirstLoop:
                Thread.Sleep(500);
                ++state.Current;
                if (state.Current > state.FirstEnd)
                {
                    state.Current = state.SecondStart;
                    state.Position = StatePosition.SecondLoop;
                }
                return true;

            case StatePosition.SecondLoop:
                Thread.Sleep(500);
                ++state.Current;
                if (state.Current > state.SecondEnd)
                {
                    state.Position = StatePosition.End;
                    return false;
                }
                return true;

            default:
                throw new InvalidOperationException("Cannot continue on a finished state machine.");
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

        while (MoveNext(myState))
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
            MyState myState = new(
                1 + mod, 5 + mod,
                1001 + mod, 1005 + mod);
            InstanceMethod(identifier, myState);
        }

        Console.WriteLine("All fin");
    }
}
