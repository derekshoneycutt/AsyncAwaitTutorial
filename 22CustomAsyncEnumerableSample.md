### 22. Creating IAsyncEnumerable/IAsyncEnumerator Implementations

Instead of the kind of weird mix of asynchronous and synchronous code
in our Produce method, we want to use IAsyncEnumerable, which allows
us to use await and yield return together more fluidly. Our first step
at this will be to construct our own IAsyncEnumerable implementation.
This will be almost identical to step 4, and in fact, let’s just update
Step 4’s IEnumerable and IEnumerator instances to IAsyncEnumerable
and IAsyncEnumerator.

The state machine inside our enumerator will be almost identical but we 
can switch the Thread.Sleep for Task.Delay calls to make it more async. 
Otherwise, we just need to follow the interfaces to make the properties and methods async.

```csharp
public class Producer(
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    CancellationToken cancellationToken)
    : IAsyncEnumerator<int>
{
    public void Reset()
    {
        _position = Position.Initial;
        Current = -1;
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
    }

    private enum Position
    {
        Initial,
        FirstLoop,
        SecondLoop,
        End
    }

    private Position _position = Position.Initial;

    public int Current { get; private set; } = -1;

    public async ValueTask<bool> MoveNextAsync()
    {
        async ValueTask<bool> FirstLoop()
        {
            if (Current <= firstEnd)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                return true;
            }

            _position = Position.SecondLoop;
            Current = secondStart;
            return await SecondLoop().ConfigureAwait(false);
        }

        async ValueTask<bool> SecondLoop()
        {
            if (Current <= secondEnd)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
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
                return await FirstLoop().ConfigureAwait(false);

            case Position.FirstLoop:
                ++Current;
                return await FirstLoop().ConfigureAwait(false);

            case Position.SecondLoop:
                ++Current;
                return await SecondLoop().ConfigureAwait(false);

            default:
                throw new InvalidOperationException();
        }
    }
}

public class ProductionEnumerable(int firstStart, int firstEnd, int secondStart, int secondEnd)
    : IAsyncEnumerable<int>
{
    public IAsyncEnumerator<int> GetAsyncEnumerator(
        CancellationToken cancellationToken)
    {
        return new Producer(firstStart, firstEnd, secondStart, secondEnd, cancellationToken);
    }
}
```

Now we update Consume to take an IAsyncEnumerable and use await foreach, which results in nicer looking code than our last step.

```csharp
async Task Consume(
    string identifier,
    IAsyncEnumerable<int> values,
    CancellationToken cancellationToken)
{
    // Update to taking an IAsyncEnumerable

    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
    {
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

And our top level code also gets updated to the new IAsyncEnumerable:

```csharp
IAsyncEnumerable<int> values = new ProductionEnumerable(
    1 + mod.Value, 5 + mod.Value,
    1001 + mod.Value, 1005 + mod.Value);
tasks.Add(Consume(identifier, values, cancellationToken));
```

In the samples, I also clean up some of the excessive cancellation token work in the Run code at this point.
This is optional.

Of course, just as we did not just stay with this big implementation of IEnuemrable, we are
not going to stay with this big implementation of IAsyncEnumerable. Rather, we can let the
compiler generate most of this work for us.

#### Navigation

[Full Sample](Samples/22CustomAsyncEnumerableSample.cs)

[Home](/)

[Previous](21CancellationTokenSample.md)

[Next](23IAsyncEnumerableIteratorSample.md)
