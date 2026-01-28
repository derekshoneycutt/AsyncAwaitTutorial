### 26. Structuring a Channels Pipeline

Now, if we move the code around a bit, we can have a more structured setup that allows easy extension of a robust data pipeline.
The basic pattern here is used in a lot of C# code utilizing Channels for communications. 

For this, first we will create a basic Producer class. This will house the Channel as a private field and offer a ReadAllAsync
method routing to the ChannelReader ReadAllAsync. It will also have a basic async method to run a bunch of producers.
In short, it will look something like this:

```csharp
class Producer(int count)
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();
    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
 
    public async Task Run(CancellationToken cancellationToken)
    {
    }
}
```

To fill this in, we will move our Produce method inside our Producer method as a private method.
For this, we will reference the private _channel field to produce values onto. We can also pull
the top-level statements for running producers and pull them into the Run with slight modifications after that.
In this sample, I also increase the Delay time to a full second for better showing.

```csharp
private async Task Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    CancellationToken cancellationToken)
{
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        await _channel.Writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        await _channel.Writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
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
    _channel.Writer.Complete();
}
```

Now, we want something similar for our consumer. We make a Consumer class, containing our Consume method
as a private method and a Run method that contains the logic to run the consumers, as we have previously
in the top-level statements. In this code, I use the same pattern as the Run of Producer to await all consumer tasks as well.

```csharp
class Consumer
{
    private async Task Consume(
        string identifier,
        IAsyncEnumerable<int> values,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
    }

    public async Task Run(
        IAsyncEnumerable<int> values,
        CancellationToken cancellationToken)
    {
        List<Task> consumers = [];
        for (int index = 1; index <= Environment.ProcessorCount * 2; ++index)
        {
            string identifier = $"Action {index}";
            consumers.Add(Consume(identifier, values, cancellationToken));
        }
        await Task.WhenAll(consumers).ConfigureAwait(false);
    }
}
```

Finally, we fill in the top level statements to create a Producer and a Consumer and run them asynchronously.

```csharp
Producer producer = new(55);
Consumer consumer = new();
_ = consumer.Run(producer.ReadAllAsync(cancellationToken), cancellationToken);
        
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

await Task.Delay(500, cancellationToken).ConfigureAwait(false);

Console.WriteLine("All fin");
```

This will give us the same sort of output as the previous steps,
but now our code is well organized into 2 classes, and this decoupling can allow us to extend it further with some clarity.

#### Navigation

[Full Sample](Samples/26StructuredChannelsSample.cs)

[Home](/)

[Previous: Standard Channels](25ChannelsSample.md)

[Next: Extending Channels Pipelines with a Middleman](27ChannelMiddlemanSample.md)

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

