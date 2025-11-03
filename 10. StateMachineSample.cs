using System.CommandLine;

namespace AsyncAwaitTutorial;


public enum StatePosition
{
    Initial,

    FirstLoop,

    SecondLoop,

    End
}

public class MyState(int firstStart, int firstMax, int secondStart, int secondMax)
{
    public StatePosition Position { get; set; } = StatePosition.Initial;

    public bool CanContinue { get; set; } = true;

    public int CurrentValue { get; set; } = -1;

    public int FirstStart { get; init; } = firstStart;

    public int FirstMax { get; init; } = firstMax;

    public int SecondStart { get; init; } = secondStart;

    public int SecondMax { get; init; } = secondMax;
}



public static class StateMachineSample
{

    public static int MoveNextMyState(MyState state)
    {
        switch (state.Position)
        {
            case StatePosition.Initial:
                Thread.Sleep(1000);
                state.Position = StatePosition.FirstLoop;
                state.CurrentValue = state.FirstStart;
                state.CanContinue = true;
                return 1;

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


    public static void Run(ParseResult parseResult)
    {
        MyState myState = new(1, 5, 101, 105);

        while (myState.CanContinue)
        {
            Console.WriteLine(MoveNextMyState(myState));
        }
    }
}
