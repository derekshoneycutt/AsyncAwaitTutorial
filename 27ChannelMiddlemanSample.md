### 27. Extending Channels Pipelines with a Middleman

One of the benefits of this highly decoupled pattern is that it is much easier to extend the data pipeline with middlemen.
Here, we will create one. This middleman will consume values from the Producer and perform some task on them prior to sending
them to the Consumer. For this, we will have our own private Channel to send modified messages on, and so it will look similar
to the Producer in structure but closer to the Consumer in code. Let’s look at the basic structure first.

```csharp
class Middleman
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();
    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
 
    public async Task Intercept(
        IAsyncEnumerable<int> values,
        CancellationToken cancellationToken)
    {
 
    }
}
```

We can take the Consume from our Consumer as a baseline, but instead of printing to the screen,
we will perform some action and write the result to our own channel.
For this sample, we will collect 2 messages together and send them as 1.
The first value will be multiplied by 100000 and added to the second value; this sum will be the value sent to Consumer.
For this, we will have a private nullable int field for the last value and a semaphore to make sure we don’t have race conditions.


```csharp
private int? _lastValue = null;
 
private readonly SemaphoreSlim _synchronize = new(1);
 
private async Task Consume(
    IAsyncEnumerable<int> values,
    CancellationToken cancellationToken)
{
    await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
    {
        await _synchronize.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_lastValue is null)
            {
                _lastValue = value;
            }
            else
            {
                await _channel.Writer.WriteAsync(
                    (100000 * _lastValue.Value) + value,
                    cancellationToken).ConfigureAwait(false);
                _lastValue = null;
            }
        }
        finally
        {
            _synchronize.Release();
        }
    }
}
```

Then let’s fill in our Intercept method defined above based basically the same as the Producer, but calling Consume here.
Let’s call it some ridiculous number of times like 666.

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
    _channel.Writer.Complete();

}
```

Finally, we update the top-level statements to place our Middleman in between the producer and consumer, intercepting all messages from producer.

```csharp
Producer producer = new(55);
Consumer consumer = new();
Middleman middleman = new();
_ = consumer.Run(middleman.ReadAllAsync(cancellationToken), cancellationToken);
_ = middleman.Intercept(producer.ReadAllAsync(cancellationToken), cancellationToken);
List<Task> tasks = [producer.Run(cancellationToken)];
```

Now, when this is run, you’ll see about half of the messages, instead clearly showing the middleman merging 2 values at a time as described.
All kinds of interesting pipeline logic can be constructed, this only serving as a basic example.

#### Navigation

[Full Sample](Samples/27ChannelMiddlemanSample.cs)

[Home](/)

[Previous: Structuring a Channels Pipeline](26StructuredChannelsSample.md)

[Next: Introduce Dataflow in the Middleman](28DataFlowMiddlemanSample.md)

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

