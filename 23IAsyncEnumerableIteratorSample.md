### 23. IAsyncEnumerable Iterator Methods

Just as we replaced the IEnumerable implementation with an IEnumerable and using yield return,
we do the exact same thing here. We instead return IAsyncEnumerable, and we mark the
method as async, using await directly in it. That is, we can use yield return and await
together in this. Knowing how it is compiled helps us understand how it works.

We should certainly also take in a CancellationToken in this method. When doing so,
we now add the EnumeratorCancellation attribute on said parameter, allowing the
compiler to take additional optimizations for us.


```csharp
async IAsyncEnumerable<int> Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        yield return value;
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        yield return value;
    }
}
```

Our Consume already handles this fine from the previous step, so we just update the top level to call this new Produce and we see the familiar output.

```csharp
IAsyncEnumerable<int> values = Produce(
    1 + mod.Value, 5 + mod.Value,
    1001 + mod.Value, 1005 + mod.Value,
    cancellationToken);
tasks.Add(Consume(identifier, values, cancellationToken));
```
We now just have a nice 2 methods for our Produce/Consume setup once again, and the output remains the same as it has the entire time so far.
You can delete the custom IAsyncEnumerator implementation at this point as we will no longer even refer to it, preferring to simply use the
iterator methods like this instead moving forward.

The only problem with what we have now is that we are still tied to a single producer and a single consumer.
We cannot support multiple producers on one channel or multiple consumers on one channel. This is explicitly still only one-to-one.

#### Navigation

[Full Sample](Samples/23IAsyncEnumerableIteratorSample.cs)

[Home](/)

[Previous: Creating IAsyncEnumerable/IAsyncEnumerator Implementations](22CustomAsyncEnumerableSample.md)

[Next: Custom Channels Implementation](24MyChannelSample.md)

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


