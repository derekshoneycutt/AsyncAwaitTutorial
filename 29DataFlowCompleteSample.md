### 29. Replace Channels Pipeline with Dataflow Blocks

Now we will refactor our entire data pipeline to use Dataflow.
Both Channels and Dataflow have many uses, sometimes overlapping. We will see
that Channels is quite a lot lighter, but requires a lot more work sometimes.
Channels is lighter and more powerful in some ways, but Dataflow can make for
very clean, maintainable data pipelines.

First, we will update our Producer to take an `ITargetBlock<int>` and send values
to it instead of maintaining its own channel. For the most part, this is
just removing code.

```csharp
class Producer(int count, ITargetBlock<int> targetBlock)
{
    private async Task Produce(
        int firstStart, int firstEnd, int secondStart, int secondEnd,
        CancellationToken cancellationToken)
    {
        for (int value = firstStart; value <= firstEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            await targetBlock.SendAsync(value, cancellationToken).ConfigureAwait(false);
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            await targetBlock.SendAsync(value, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        List<Task> productionTasks = [];
        for (int index = 0; index < count; ++index)
        {
            int mod = 10 * index;
            productionTasks.Add(Produce(
                1 + mod, 5 + mod,
                1001 + mod, 1005 + mod,
                cancellationToken));
        }
        await Task.WhenAll(productionTasks).ConfigureAwait(false);
    }
}
```

Now we build the Dataflow Blocks and the whole data pipeline. The first step
is to setup some basic options objects. This is optional, but we use the time to highlight
some of the options. One of the most interesting missing is that TaskScheduler is
available for most of these options as well. This can be used to ensure that Tasks
are scheduled on the appropriate context.

The code from here can just be in the main Run code.

```csharp
DataflowBlockOptions blockOptions = new()
{
    CancellationToken = cancellationToken,
    BoundedCapacity = Environment.ProcessorCount * 2,
    MaxMessagesPerTask = 5
};

GroupingDataflowBlockOptions groupingOptions = new()
{
    CancellationToken = cancellationToken,
    BoundedCapacity = Environment.ProcessorCount * 2,
    Greedy = true,
    MaxMessagesPerTask = 5
};

ExecutionDataflowBlockOptions executionOptions = new()
{
    CancellationToken = cancellationToken,
    BoundedCapacity = Environment.ProcessorCount * 2,
    SingleProducerConstrained = false,
    MaxMessagesPerTask = 5,
    MaxDegreeOfParallelism = Environment.ProcessorCount * 2
};
```

Now we construct the basic pipeline. We can start with what we had in the Middleman
and expand it further. For this, we will have the BufferBlock,
BatchBlock, and TransformBlock again. After this, we will also add a
BroadcastBlock and 3 ActionBlocks. The BroadcastBlock will send each message
to all 3 ActionBlocks, which will print each value uniquely.

```csharp
BufferBlock<int> buffer = new(blockOptions);

BatchBlock<int> batcher = new(2, groupingOptions);
buffer.LinkTo(batcher);

TransformBlock<int[], int> transform = new(values =>
    (100000 * values[0]) + (values.Length > 0 ? values[1] : 0), executionOptions);
batcher.LinkTo(transform);

BroadcastBlock<int> broadcast = new(null, blockOptions);
transform.LinkTo(broadcast);

ActionBlock<int> writer = new(value =>
    Console.WriteLine($"Writer 1 / {Environment.CurrentManagedThreadId} => {value}", executionOptions));
broadcast.LinkTo(writer);

ActionBlock<int> writer2 = new(value =>
    Console.WriteLine($"Writer 2 / {Environment.CurrentManagedThreadId} => {value}", executionOptions));
broadcast.LinkTo(writer2);

ActionBlock<int> writer3 = new(value =>
    Console.WriteLine($"Writer 3 / {Environment.CurrentManagedThreadId} => {value}", executionOptions));
broadcast.LinkTo(writer3);
```

What remains is to also Complete each of these Blocks at the end, to ensure
that everything is cleaned up safely.

```csharp
Producer producer = new(55, buffer);
        
List<Task> tasks = [producer.Run(cancellationToken)];

await Task.Delay(500, cancellationToken).ConfigureAwait(false);
TaskCompletionSource backThreadSource = new();
Thread instanceCaller = new(new ThreadStart(() =>
    DoubleLoop("Single Thread",
        1, 5,
        101, 105,
        backThreadSource, cancellationToken)));
instanceCaller.Start();
tasks.Add(backThreadSource.Task);

await Task.WhenAll(tasks).ConfigureAwait(false);

buffer.Complete();
batcher.Complete();
transform.Complete();
broadcast.Complete();
writer.Complete();
writer2.Complete();
writer3.Complete();

await Task.Delay(500, cancellationToken).ConfigureAwait(false);

Console.WriteLine("All fin");
```

And this provides us with a new asynchronous pipeline. With many values produced,
we merge them 2 at a time and print them 3 times to the screen.

There are different times for the different tools we have demonstrated here,
from iterators to asynchronous tasking, channels, and Dataflow.

The Dataflow especially has more tools that can be utilized, including
the ability to set a TaskScheduler to control how individual tasks are run
in different Blocks.

#### Navigation

[Full Sample](Samples/29DataFlowCompleteSample.cs)

[Home](/)

[Previous: Introduce Dataflow in the Middleman](28DataFlowMiddlemanSample.md)

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

