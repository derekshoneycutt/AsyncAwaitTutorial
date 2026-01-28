### 20. Introducing IAsyncDisposable for CancellationTokenRegistration

The standard CancellationToken has many features we have not implemented
in our quick example. One of these includes a structure that allows us
to easily unregister callbacks that have been registered. This structure
implements the standard IDisposable interface, and it also provides
a good time to introduce the asynchronous IAsyncDisposable.

First, we add an Unregister method to our MyCancellationTokenSource.

```csharp
public void Unregister(Action callback)
{
    lock (_callbacks)
    {
        if (!_isCancellationRequested && _callbacks.Contains(callback))
        {
            _callbacks.Remove(callback);
        }
    }
}
```

Next, we will create a small struct called `MyCancellationTokenRegistration`.
This will take a `MyCancellationTokenSource` and a callback `Action` in
the primary constructor. It will implement both `IDisposable` and
`IAsyncDisposable`. We will not include the full dispose pattern here,
as that is well beyond needs of this tutorial.

This gives us 2 methods to implement: Dispose and DisposeAsync. DisposeAsync
returns a ValueTask, which can also be used in async methods much like
Task. However, this provides some optimizations in particular scenarios.
Most will not use ValueTask regularly, but IAsyncDisposable is a common case.

```csharp
public readonly struct MyCancellationTokenRegistration(
    MyCancellationTokenSource source, Action callback)
    : IDisposable, IAsyncDisposable
{
    public void Dispose()
    {
        source.Unregister(callback);
    }

    public async ValueTask DisposeAsync()
    {
        source.Unregister(callback);
    }
}
```

Now, we update our Register method to return one of these structures,
allowing callbacks to be unregistered via this interface.

```csharp
public MyCancellationTokenRegistration Register(Action callback)
{
    lock (_callbacks)
    {
        if (!_isCancellationRequested)
        {
            _callbacks.Add(callback);
            return new(this, callback);
        }
    }

    callback();
    return new(this, callback);
}
```


Now we can handle this in our Run method where we add a 
registration. For example, we can now use `await using` and have
DisposeAsync called appropriately.

```csharp
await using MyCancellationTokenRegistration cancelRegister = cts.Register(() =>
{
    Console.WriteLine("Registered cancellation.");
});
```

In addition, we can call Dispose or DisposeAsync anywhere we wish
to unregister at as well.

```csharp
await cancelRegister.DisposeAsync().ConfigureAwait(false);
```

This step is mostly expanded to introduce the concept of the
`IAsyncDisposable` interface. Knowing it, we can now move on to
the standard cancellation token comfortably.

#### Navigation

[Full Sample](Samples/20IAsyncDisposableSample.cs)

[Home](/)

[Previous: Constructing Cancellation Tokens](19MyCancellationTokenSample.md)

[Next: Standard Cancellation Tokens](21CancellationTokenSample.md)

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


