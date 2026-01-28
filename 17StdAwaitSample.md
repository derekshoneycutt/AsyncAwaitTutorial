### 17. Standard async/await

We now remove our custom task classes entirely, and use just the
standard Task classes. It will become immediately apparent that
we do not have all the tools we want for this yet, as Produce
will need more refactoring.

To begin, we create a DelayOnNumber method. This just delays for
a second before returning a given value. This will be an async
method, and the Task representing the operation is what our
new Produce will return each iteration.

While doing this, we want to also begin to appropriately use
the ConfigureAwait method on the standard Task object. By default,
every Task is set as if you called `ConfigureAwait(true)`. This
causes the current task to attempt to return to the same execution
context that it started on before awaiting on a called Task.
However, this can be a performance hit, and in the vast majority
of non-UI library code, you want to explicitly call `ConfigureAwait(false)`
when you await a Task, such that you can continue the rest of the
method on any thread, or any context. You may utilize the default
behavior to stay on the UI thread without using Dispatcher, which
makes this extremely useful in some cases, however.

```csharp
async Task<int> DelayOnNumber(
    int number)
{
    await Task.Delay(1000).ConfigureAwait(false);
    return number;
}
```

The updated Produce method now looks nicer, although the
DelayOnNumber thing is not our favorite.

```csharp
IEnumerable<Task<int>> Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd)
{
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        yield return DelayOnNumber(value);
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        yield return DelayOnNumber(value);
    }
}
```

Consume method simply changes to using all standard Tasks,
and we also add in `ConfigureAwait(false)` here.

```csharp
async Task Consume(
    string identifier,
    IEnumerable<Task<int>> values)
{
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    foreach (Task<int> valueTask in values)
    {
        int value = await valueTask.ConfigureAwait(false);
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

We also can use `ConfigureAwait(false)` in our Run code if we want.

```csharp
await Task.WhenAll(tasks).ConfigureAwait(false);
```

We now have standard async/await code, and we should understand
it pretty clearly through this work. We have several pieces to
improve on yet, but this is very strong code.

#### Navigation

[Full Sample](Samples/17StdAwaitSample.cs)

[Home](/)

[Previous: Using actual async/await with MyTask](16AwaitableCustomSample.md)

[Next: Task Completion Source](18TaskCompletionSourceSample.md)

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

