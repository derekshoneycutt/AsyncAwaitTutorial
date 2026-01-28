###  28. Introduce Dataflow in the Middleman

Up to now, we have highlighted a lot of Producer/Consumer pattern as we approached
asynchronous patterns. This has led to the creation of a data pipeline with an
immense amount of control at each step. In the standard C# Task library, we also
have an additional tool for data pipelines called Dataflow. This separates pieces
of a data pipeline into blocks that can be linked one to another.

We will change our Middleman from the Channels pipeline to use 3 Block types from
Dataflow to do the same task we were doing before. This makes for a simple introduction.

In our Middleman, we declare 3 fields. First, a BufferBlock to receive all of the
values from our producers. Next, this will go into a BatchBlock that combines
the messages into blocks of 2. Finally, this will go into a TransformBlock
that combines the 2 values at a time into single value.

```csharp
private readonly BufferBlock<int> _buffer;

private readonly BatchBlock<int> _batch;

private readonly TransformBlock<int[], int> _transform;
```

To link these, we need to make a constructor that constructs these Blocks
with the appropriate options and links them together. We will send in a
CancellationToken that can cancel the pipeline in this constructor.
Some of the options used to construct the Blocks here are entirely optional.
We include them here just to show some of the easier options.

```csharp
public Middleman(
    CancellationToken cancellationToken)
{
    _buffer = new(new()
    {
        CancellationToken = cancellationToken,
        BoundedCapacity = Environment.ProcessorCount * 2
    });
    _batch = new(2, new()
    {
        CancellationToken = cancellationToken,
        BoundedCapacity = Environment.ProcessorCount * 2,
        Greedy = true,
        MaxMessagesPerTask = 5
    });
    _transform = new(values =>
        (100000 * values[0]) + (values.Length > 0 ? values[1] : 0), new()
        {
            CancellationToken = cancellationToken,
            BoundedCapacity = Environment.ProcessorCount * 2,
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
            MaxMessagesPerTask = 5,
            SingleProducerConstrained = false
        });


    _buffer.LinkTo(_batch);
    _batch.LinkTo(_transform);
}
```

We will then also update our ReadAllAsync to read from the TransformBlock and
our Consume to send directly to the BufferBlock.

```csharp
public IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken) =>
    _transform.ReceiveAllAsync(cancellationToken);

private async Task Consume(
    IAsyncEnumerable<int> values,
    CancellationToken cancellationToken)
{
    await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
    {
        await _buffer.SendAsync(value, cancellationToken).ConfigureAwait(false);
    }
}
```

Finally, we update Intercept to Complete each of the Dataflow blocks
once the Intercept option is completed.

```csharp
public async Task Intercept(
    IAsyncEnumerable<int> values,
    CancellationToken cancellationToken)
{
    List<Task> consumers = [];
    for (int index = 1; index <= 666; ++index)
    {
        consumers.Add(Consume(
            values,
            cancellationToken));
    }
    await Task.WhenAll(consumers).ConfigureAwait(false);
    _buffer.Complete();
    _batch.Complete();
    _transform.Complete();
}
```

This appears to simplify our Middleman quite a lot, and we could easily
consider expanding the use of Dataflow blocks in our pipeline. It is worth
noting that we did lose a lot of low level control, but in its place,
the code is remarkably pleasant and maintainable.

#### Navigation

[Full Sample](Samples/28DataFlowMiddlemanSample.cs)

[Home](/)

[Previous: Structuring a Channels Pipeline](27ChannelMiddlemanSample.md)

[Next: Replace Channels Pipeline with Dataflow Blocks](29DataFlowCompleteSample.md)

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

