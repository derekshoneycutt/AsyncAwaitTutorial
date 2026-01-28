### 5. Use Iterator Methods

Those who have been around C# for a while are well aware of yield return and might just be thinking,
“Can’t we just use an iterator method instead of this whole IEnumerable structure?” The answer is yes.
We will go ahead and make the iterator method for this. The compiler will essentially make the same code
we just did for us. Note: keep the manual instance around as we will update it to async in the future!

Note: I would suggest commenting and saving the IEnumerable and IEnumerator implementations for future
use when we have async better established.

The iterator method should just take two ranges again and loop through them, performing a Sleep and then
yield return for each value. This is extremely close to the Produce method we had before, and we can just
update it to IEnumerable, yield return, and including the Thread.Sleep now if we have it around still.

```csharp
IEnumerable<int> Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd)
{
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        Thread.Sleep(500);
        yield return value;
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        Thread.Sleep(500);
        yield return value;
    }
}
```

Our Consume method already handles this perfectly fine, so we just need to call Produce on the top level
instead of making a ProductionEnumerator instance:

```csharp
IEnumerable<int> values = Produce(
    1 + mod, 5 + mod,
    1001 + mod, 1005 + mod);
Consume(identifier, values);
```

Again, this has the same output as before, but now we have 2 extremely clean methods. One method
produces values, and the other consumes and prints them to the screen. This is the basic pattern we want
to capitalize on in our code moving forward.

The one big drawback that we have now, however, is that we are not asynchronous. Everything in this code
all happens on the same thread, in series. We want to make some parallelism possible! Unfortunately,
we do not even having threading explored in this tutorial yet, so we have some ways to go.

#### Navigation

[Full Sample](Samples/05IteratorProducerSample.cs)

[Home](/)

[Previous: Use IEnumerable/IEnumerator Interfaces](04IEnumerableProducerSample.md)

[Next: Make it Multithreaded](06ThreadSample.md)

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

