namespace AsyncAwaitTutorial;





/// <summary>
/// This sample demonstrates creating a basic state machine
/// </summary>
public static class StateMachineSample
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
    public class MyState(int firstStart, int firstMax, int secondStart, int secondMax)
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
        public int FirstMax { get; init; } = firstMax;

        /// <summary>
        /// Gets the start of the second range to loop through.
        /// </summary>
        public int SecondStart { get; init; } = secondStart;

        /// <summary>
        /// Gets the max of the second range to loop through.
        /// </summary>
        public int SecondMax { get; init; } = secondMax;
    }


    /// <summary>
    /// Moves to the next position in the state machine.
    /// </summary>
    /// <param name="state">The state to advance to the next position.</param>
    /// <returns>The current value of the state machine</returns>
    /// <exception cref="InvalidOperationException">Cannot continue on a finished state machine.</exception>
    public static int MoveNextMyState(MyState state)
    {
        switch (state.Position)
        {
            case StatePosition.Initial:
                Thread.Sleep(1000);
                state.Position = StatePosition.FirstLoop;
                state.CurrentValue = state.FirstStart;
                state.CanContinue = true;
                return state.CurrentValue;

            case StatePosition.FirstLoop:
                Thread.Sleep(1000);
                ++state.CurrentValue;
                if (state.CurrentValue > state.FirstMax)
                {
                    state.Position = StatePosition.SecondLoop;
                    state.CurrentValue = state.SecondStart;
                }
                return state.CurrentValue;

            case StatePosition.SecondLoop:
                Thread.Sleep(1000);
                ++state.CurrentValue;
                if (state.CurrentValue >= state.SecondMax)
                {
                    state.Position = StatePosition.End;
                    state.CanContinue = false;
                }
                return state.CurrentValue;

            default:
                throw new InvalidOperationException("Cannot continue on a finished state machine.");
        }
    }



    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    public static void Run()
    {
        MyState myState = new(1, 5, 101, 105);

        while (myState.CanContinue)
        {
            Console.WriteLine(MoveNextMyState(myState));
        }
    }
}
