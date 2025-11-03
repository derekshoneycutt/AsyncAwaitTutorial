using System.Collections;
using System.CommandLine;

namespace AsyncAwaitTutorial;


public enum EnumeratorStatePosition
{
    Initial,

    FirstLoop,

    SecondLoop,

    End
}


public class MyEnumerator(int firstStart, int firstMax, int secondStart, int secondMax)
    : IEnumerator<int>
{
    private EnumeratorStatePosition _position = EnumeratorStatePosition.Initial;

    private int _currentValue = 0;

    public bool MoveNext()
    {
        switch (_position)
        {
            case EnumeratorStatePosition.Initial:
                Thread.Sleep(1000);
                _position = EnumeratorStatePosition.FirstLoop;
                _currentValue = firstStart;
                return true;

            case EnumeratorStatePosition.FirstLoop:
                Thread.Sleep(1000);
                ++_currentValue;
                if (_currentValue > firstMax)
                {
                    _currentValue = secondStart;
                    _position = EnumeratorStatePosition.SecondLoop;
                }
                return true;

            case EnumeratorStatePosition.SecondLoop:
                Thread.Sleep(1000);
                ++_currentValue;
                if (_currentValue > secondMax)
                {
                    _position = EnumeratorStatePosition.End;
                    return false;
                }
                return true;

            default:
                throw new InvalidOperationException("Cannot continue on a finished state machine.");
        }
    }

    public void Reset()
    {
        _position = EnumeratorStatePosition.Initial;
        _currentValue = 0;
    }

    object IEnumerator.Current
    {
        get
        {
            return _currentValue;
        }
    }

    public int Current
    {
        get
        {
            return _currentValue;
        }
    }

    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

public class MyEnumerable(int firstStart, int firstMax, int secondStart, int secondMax)
    : IEnumerable<int>
{



    public IEnumerator<int> GetEnumerator()
    {
        return new MyEnumerator(firstStart, firstMax, secondStart, secondMax);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}


public static class IEnumerableSample
{


    public static void Run(ParseResult parseResult)
    {
        MyEnumerable myState = new(1, 5, 101, 105);

        //List<int> listed = [.. myState]; // Same as below...

        foreach (int value in myState)
        {
            Console.WriteLine(value);
        }
    }
}
