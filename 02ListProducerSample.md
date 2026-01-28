### 2. First Producer: Produce a List

To begin, we will show some basic language features that will be important for
our asynchronous code later. For now, it will help us to decouple our code and
get used to some basic patterns.

First on order is to split up our code so that we have a method that produces
the values to display, and another method to display them. This is a common separation
made in many graphical applications, so it should be pretty simple for us still.

We will replace our current DoubleLoop method with new Produce and Consume methods.
Let's start with Produce.

At this stage, Produce will just take in the 2 ranges and return a List containing
all of the specified values.

```csharp
List<int> Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd)
{
    List<int> values = [];

    for (int value = firstStart; value <= firstEnd; ++value)
    {
        values.Add(value);
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        values.Add(value);
    }

    return values;
}
```

Consume then simply takes in the collection--we will use `IEnumerable<int>` immediately
here as it will be more flexible for future updates--and loops through each value,
printing it to the screen. This is now only a single loop!

```csharp
public static void Consume(
    string identifier,
    IEnumerable<int> values)
{
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    foreach (int value in values)
    {
        Thread.Sleep(500);
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

We must also update the run code for this to call Produce and then Consume.

```csharp
for (int index = 1; index <= 5; ++index)
{
    int mod = 10 * index;
    string identifier = $"Action {index}";
    List<int> values = Produce(
        1 + mod, 5 + mod,
        1001 + mod, 1005 + mod);
    Consume(identifier, values);
}
```

This gives us a really basic Producer/Consumer pattern, and in a way that is quite common
in C#. This is great for making easier, more maintainable code. However, this version
has many limitations. Namely, we have to wait for all of the values to be produced and then
print them. We maintain a Sleep in our Consume, but this does not simulate the common
cases. For example, if the values come from a web API call, the delay would be in the Produce.
In this setup, we would have to wait for all such delays to complete before we print them.
We will need to make it so we can consume values as they are produced, and this simple method
does not do the job yet.

#### Navigation

[Full Sample](Samples/02ListProducerSample.cs)

[Home](/)

[Previous: Simple Procedural Code](01ProceduralSample.md)

[Next: Making Producer Sleep: State Machine](03StateMachineProducerSample.md)

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
