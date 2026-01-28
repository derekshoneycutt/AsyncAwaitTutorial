### 6. Make it Multithreaded

The first step to now make this asynchronous is to use Threads. We will simply update our code so that
each call to Consume runs on its own thread. We will see the weakness of this approach and build
something better as we progress.

For this step, we create a `List<Thread>` and initialize it as empty. Then inside our Run loop,
we launch the `Consume` method in a new `Thread` that is added to the list. At the end, we then
add a second loop that Joins each thread, effectively waiting for each instance to complete.

```csharp
List<Thread> threads = []; // Store threads spun off here
for (int index = 1; index <= 5; ++index)
{
    int mod = 10 * index;
    string identifier = $"Action {index}";
    // Create and start a thread, adding it to the collection
    Thread thread = new(new ThreadStart(() =>
    {
        IEnumerable<int> values = Produce(
            1 + mod, 5 + mod,
            1001 + mod, 1005 + mod);
        Consume(identifier, values);
    }));
    thread.Start();
    threads.Add(thread);
}

// Join all the stored threads to the current before finishing.
foreach (Thread thread in threads)
{
    thread.Join();
}
```

This is the first time our output is significantly changed. Now we have multiple streams
of production being consumed at the same time, instead of one after the other.
However, while concurrency is nice, this is not truly asynchronous. We need to build the
patterns and concepts for asynchrony.

#### Navigation

[Full Sample](Samples/06ThreadSample.cs)

[Home](/)

[Previous: Use Iterator Methods](05IteratorProducerSample.md)

[Next: Make a Custom Thread Pool](07MyThreadPoolSample.md)

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

