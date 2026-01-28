### 25. Standard Channels

Now, take out our custom `MyChannel<T>` class and just use the standard channels.

Our Consumer remains perfect, we don’t need to change it. However, our Producer now needs to take in a standard
`ChannelWriter<T>` and use the WriteAsync method it provides.

```csharp
async Task Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    ChannelWriter<int> channel,
    CancellationToken cancellationToken)
{
    // Update to the standard channel writer
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        await channel.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        await channel.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }
}
```

We then just update our top level statement to use CreateUnbounded. We could also create Bounded,
PrioritizedUnbounded, and play with the many options that are available in the standard channels,
but we will just keep it simple and use Unbounded for now. Note that this has Reader and Writer
properties that must be referenced instead of the `Channel<T>` directly for all operations, so we make that change as well.

```csharp
 Channel<int> channel = Channel.CreateUnbounded<int>();

for (int index = 1; index <= Environment.ProcessorCount * 2; ++index)
{
    string identifier = $"Action {index}";
    _ = Consume(identifier, channel.Reader.ReadAllAsync(cancellationToken), cancellationToken);
}

List<Task> tasks = [];
for (int index = 1; index <= 55; ++index)
{
    int mod = 10 * index;
    string identifier = $"Action {index}";
    tasks.Add(Produce(
        1 + mod, 5 + mod,
        1001 + mod, 1005 + mod,
        channel,
        cancellationToken));
}

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
channel.Writer.Complete();

await Task.Delay(500, cancellationToken).ConfigureAwait(false);

Console.WriteLine("All fin");
```

And this completes our work with channels! We now are effectively using the Producer/Consumer pattern with highly
async code and supporting multiple producers and multiple consumers on the same channel.

#### Navigation

[Full Sample](Samples/25ChannelsSample.cs)

[Home](/)

[Previous: Custom Channels Implementation](24MyChannelSample.md)

[Next: Structuring a Channels Pipeline](26StructuredChannelsSample.md)

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

