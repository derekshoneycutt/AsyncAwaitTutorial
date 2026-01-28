### 9. Use the Standard Thread Pool

Now we know enough about how the Thread Pool works, we can just switch over to the
standard ThreadPool, which includes several optimizations and makes our code cleaner
due to not having the ThreadPool around on us.

Simply, we can just comment out or delete the MyThreadPool class. Then use
`ThreadPool.QueueUserWorkItem`. Note, this requires a state variable parameter in
the queued action, which we did not add in ours. Lets utilize this to optimize
our thread, however. We will create a readonly record struct `ThreadPoolState`
that takes a String identifier and a async local int Mod value.

```csharp
readonly record struct ThreadPoolState(string Identifier, AsyncLocal<int> Mod);
```

Then, instead of using the captured lambda state, we pass a new instance of this
record into the QueueUserWorkItem, using the generic version of the method
for type safety.

```csharp
ThreadPool.QueueUserWorkItem<ThreadPoolState>(state =>
{
    IEnumerable<int> values = Produce(
        1 + state.Mod.Value, 5 + state.Mod.Value,
        1001 + state.Mod.Value, 1005 + state.Mod.Value);
    Consume(state.Identifier, values);
}, new(identifier, mod), true);
```

This is a very simple step after our work with a custom thread pool, but now
we are ready to begin building a Task structure that tracks work done on the
thread pool, instead of using our counter and reset event.

#### Navigation

[Full Sample](Samples/09ThreadPoolSample.cs)

[Home](/)

[Previous: Handle Thread Local Storage and Execution Contexts](08MyThreadPoolWithContextSample.md)

[Next: Custom Task Completion Class](10MyTaskCompletionSample.md)

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

