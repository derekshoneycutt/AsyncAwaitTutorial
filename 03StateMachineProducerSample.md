### 3. Making Producer Sleep: State Machine

One of the most common structures that enables us to consume values as they are produced is
the state machine. This is a broad kind of structure that is used throughout many
programming challenges. In fact, many modern language features, including in C#, ultimately
are compiled down into forms of state machines.

In short, a state machine is just a representation of computation that is always in
one of many recognized positions called states. Some state machines have several actions
that can result in advancing the computation from one state to another. The simplest state
machines may simply have a value containing the current state and a Next method to advance
to the next state. The common Iterator is a form of this simple state machine. We will
construct such a simple state machine to show how we can consume values as they are produced.

We start by creating a basic class, which we call MyState for now. We put 2 range fields in 
our primary constructor: the two ranges to loop through. We also have a privately mutable 
property Current and a method MoveNext that returns a Boolean indicating if Current contains
a valid value. Note if your following this tutorial: we can keep our Produce method around,
we will return to it later; the sample code maintains it in a previous sample so removes it.

```csharp
class Producer(int firstStart, int firstEnd, int secondStart, int secondEnd)
{
    public int Current { get; private set; } = -1;
 
    public bool MoveNext()
    {
        // What to do?
    }
}
```

Since MoveNext will not actually contain a real loop, we need to track where in our 2
“loops” we are to “move next” each time MoveNext is called. Inside the MyState class, we
create a private enum Position with values Initial, FirstLoop, SecondLoop, and End,
and create a private property starting at Initial.

```csharp
private enum Position
{
    Initial,
    FirstLoop,
    SecondLoop,
    End
}
 
private Position _position = Position.Initial;
```

Now, we just need to fill out MoveNext. We can use a switch on _position to know where
we are. For each loop, we create small methods that handle a single iteration of each loop
and move forward. It is in here that we will perform our Sleep from the original method.
In the case we reach the end, return false. If we are at the end when MoveNext is called,
throw an exception.

```csharp
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
```

This completes the state machine for this step.
Now, we modify Consume to take the Producer object. We also change the loop to a while
loop that continues if MoveNext() returns true. The value to print on each iteration
will then be the Current property of the Producer. The producer does the sleep for us,
so we remove that from this method as well.

```csharp
void Consume(
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
```

Finally, we update our running code to create a Producer for each instance and pass it
into the Consume method. The result is identical output to what we had before, but we have
now created a pattern of separate production and consumption that allows consuming
as values are produced!

```csharp
for (int index = 1; index <= 5; ++index)
{
    int mod = 10 * index;
    string identifier = $"Action {index}";
    Producer producer = new(
        1 + mod, 5 + mod,
        1001 + mod, 1005 + mod);
    Consume(identifier, producer);
}
```

#### Navigation

[Full Sample](Samples/03StateMachineProducerSample.cs)

[Home](/)

[Previous: First Producer: Produce a List](02ListProducerSample.md)

[Next: Use IEnumerable/IEnumerator Interfaces](04IEnumerableProducerSample.md)

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
