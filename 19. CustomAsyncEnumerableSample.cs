using System.CommandLine;

namespace AsyncAwaitTutorial;

public enum AsyncEnumeratorStatePosition
{
    Initial,

    FirstLoop,

    SecondLoop,

    End
}

public class MyAsyncEnumerator(int firstStart, int firstMax, int secondStart, int secondMax)
    : IAsyncEnumerator<int>
{
    private AsyncEnumeratorStatePosition _position = AsyncEnumeratorStatePosition.Initial;

    private int _currentValue = 0;


    public int Current => _currentValue;

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        switch (_position)
        {
            case AsyncEnumeratorStatePosition.Initial:
                await Task.Delay(1000).ConfigureAwait(false);
                _position = AsyncEnumeratorStatePosition.FirstLoop;
                _currentValue = firstStart;
                return true;

            case AsyncEnumeratorStatePosition.FirstLoop:
                await Task.Delay(1000).ConfigureAwait(false);
                ++_currentValue;
                if (_currentValue > firstMax)
                {
                    _currentValue = secondStart;
                    _position = AsyncEnumeratorStatePosition.SecondLoop;
                }
                return true;

            case AsyncEnumeratorStatePosition.SecondLoop:
                await Task.Delay(1000).ConfigureAwait(false);
                ++_currentValue;
                if (_currentValue > secondMax)
                {
                    _position = AsyncEnumeratorStatePosition.End;
                    return false;
                }
                return true;

            default:
                throw new InvalidOperationException("Cannot continue on a finished state machine.");
        }
    }
}

public class MyAsyncEnumerable(int firstStart, int firstMax, int secondStart, int secondMax)
    : IAsyncEnumerable<int>
{
    public IAsyncEnumerator<int> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
    {
        return new MyAsyncEnumerator(firstStart, firstMax, secondStart, secondMax);
    }
}


public static class CustomAsyncEnumerableSample
{


    public static async Task Run(ParseResult parseResult, CancellationToken cancellationToken)
    {
        MyAsyncEnumerable myState = new(1, 5, 101, 105);

        await foreach (int value in myState)
        {
            Console.WriteLine(value);
        }
    }
}
