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

[Previous: Standard Cancellation Tokens](21CancellationTokenSample.md)

[Next: IAsyncEnumerable Iterator Methods](23IAsyncEnumerableIteratorSample.md)

#### Full Navigation

##### 1. Conceptual Setup

1. [Simple Procedural Code](01ProceduralSample.md)
1. [First Producer: Produce a List](02ListProducerSample.md)
1. [Making Producer Sleep: State Machine](03StateMachineProducerSample.md)
1. [Use IEnumerable/IEnumerator Interfaces](04IEnumerableProducerSample.md)
1. [Use Iterator Methods](05IteratorProducerSample.md)

##### 2. Multithreading

6. [Make it Multithreaded](06ThreadSample.md)
1. [Make a Custom Thread Pool](07MyThreadPoolSample.md)
1. [Handle Thread Local Storage and Execution Contexts](08MyThreadPoolWithContextSample.md)
1. [Use the Standard Thread Pool](09ThreadPoolSample.md)

##### 3. Tasking Structure

10. [Custom Task Completion Class](10MyTaskCompletionSample.md)
1. [Custom Task Class](11MyTaskSample.md) 
1. [Implementing ContinueWith and WhenAll](12MyTaskWhenAllSample.md)
1. [Implementing Delay and Task&lt;TResult&gt;](13MyTaskDelaySample.md)

##### 4. Async/Await

14. [Creating an Asynchronous Chain with ContinueWith](14MyTaskAsyncChainSample.md)
1. [Simulate async/await with Iterators](15IterateTaskGeneratorSample.md)
1. [Using actual async/await with MyTask](16AwaitableCustomSample.md)
1. [Standard async/await](17StdAwaitSample.md)

##### 5. Asynchronous Utilities

18. [Task Completion Source](18TaskCompletionSourceSample.md)
1. [Constructing Cancellation Tokens](19MyCancellationTokenSample.md) 
1. [Introducing IAsyncDisposable for CancellationTokenRegistration](20IAsyncDisposableSample.md)
1. [Standard Cancellation Tokens](21CancellationTokenSample.md)
1. [Creating IAsyncEnumerable/IAsyncEnumerator Implementations](22CustomAsyncEnumerableSample.md)
1. [IAsyncEnumerable Iterator Methods](23IAsyncEnumerableIteratorSample.md)

##### 6. Asynchronous Channels

24. [Custom Channels Implementation](24MyChannelSample.md)
1. [Standard Channels](25ChannelsSample.md)
1. [Structuring a Channels Pipeline](26StructuredChannelsSample.md)
1. [Extending Channels Pipelines with a Middleman](27ChannelMiddlemanSample.md)

##### 7. Dataflow

28. [Introduce Dataflow in the Middleman](28DataFlowMiddlemanSample.md)
1. [Replace Channels Pipeline with Dataflow Blocks](29DataFlowCompleteSample.md)

