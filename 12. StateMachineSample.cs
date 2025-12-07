namespace AsyncAwaitTutorial;





/// <summary>
/// This sample demonstrates creating a basic state machine
/// </summary>
/// <remarks>
/// This is a major tangent from prior code to demonstrate the basic concepts of state machines and iterators.
/// We basically take Step 1 -- Procedural Sample -- and refactor it into a C-style state machine.
/// We run though a single instance of the state process in the Run method.
/// </remarks>
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
        /// Gets or sets whether the state machine can continue to process another step.
        /// </summary>
        public bool CanContinue { get; set; } = true;

        /// <summary>
        /// Gets or sets the current value represented by the state.
        /// </summary>
        public int CurrentValue { get; set; } = -1;

        /// <summary>
        /// Gets the start of the first range to loop through.
        /// </summary>
        public int FirstStart { get; init; } = firstStart;

        /// <summary>
        /// Gets the max of the first range to loop through.
        /// </summary>
        public int FirstEnd { get; init; } = firstEnd;

        /// <summary>
        /// Gets the start of the second range to loop through.
        /// </summary>
        public int SecondStart { get; init; } = secondStart;

        /// <summary>
        /// Gets the max of the second range to loop through.
        /// </summary>
        public int SecondEnd { get; init; } = secondEnd;
    }


    /// <summary>
    /// Moves to the next position in the state machine.
    /// </summary>
    /// <param name="state">The state to advance to the next position.</param>
    /// <returns>The current value of the state machine</returns>
    /// <exception cref="InvalidOperationException">Cannot continue on a finished state machine.</exception>
    public static bool MoveNextMyState(MyState state)
    {
        switch (state.Position)
        {
            case StatePosition.Initial:
                Thread.Sleep(500);
                state.Position = StatePosition.FirstLoop;
                state.CurrentValue = state.FirstStart;
                return true;

            case StatePosition.FirstLoop:
                Thread.Sleep(500);
                ++state.CurrentValue;
                if (state.CurrentValue > state.FirstEnd)
                {
                    state.CurrentValue = state.SecondStart;
                    state.Position = StatePosition.SecondLoop;
                }
                return true;

            case StatePosition.SecondLoop:
                Thread.Sleep(500);
                ++state.CurrentValue;
                if (state.CurrentValue > state.SecondEnd)
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
    /// <param name="firstStart">The first start value.</param>
    /// <param name="firstEnd">The first maximum value, completing the first range.</param>
    /// <param name="secondStart">The second start value.</param>
    /// <param name="secondEnd">The second maximum value, completing the second range.</param>
    public static void InstanceMethod(
        string identifier,
        int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        MyState myState = new(1, 5, 101, 105);

        while (MoveNextMyState(myState))
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {myState.CurrentValue}");
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
