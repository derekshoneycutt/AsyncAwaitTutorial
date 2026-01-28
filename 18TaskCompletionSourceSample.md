### 18. Task Completion Source

The first thing we want to do is show how we can use the standard
classes to achieve some of the same patterns we were doing with
our custom Task at times. We frequently created a Task and then
did SetResult or SetException in another process to signal
when it was completed. The standard Task classes do not allow us
to do this directly, but a separate `TaskCompletionSource` was
created that enables this pattern for us.

A good example of where this might be useful is some long running
process that is better served on a managed, dedicated thread. Any
operation potentially leading to thread exhaustion might be better
served in this pattern in async code.

We will re-create our DoubleLoop from our first procedural sample
that got us started. This time, it will take a TaskCompletionSource
and signal to the source when it is completed or an exception has occurred.

```csharp
public static void DoubleLoop(
    string identifier,
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    TaskCompletionSource completionSource)
{
    // Almost identical to step 1's DoubleLoop, but completionSource is a TaskCompletionSource.
    try
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        for (int value = firstStart; value <= firstEnd; ++value)
        {
            Thread.Sleep(1000);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            Thread.Sleep(1000);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");

        completionSource.SetResult();
    }
    catch (Exception ex)
    {
        completionSource.SetException(ex);
    }
}
```

Now, we just launch this as a separate thread and we can await
on the Task provided by the TaskCompletionSource.

```csharp
List<Task> tasks = [];
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    IEnumerable<Task<int>> values = Produce(
        1 + mod.Value, 5 + mod.Value,
        1001 + mod.Value, 1005 + mod.Value);
    tasks.Add(Consume(identifier, values));
}

// We delay a short time and then spin off a background thread, with a ThreadCompletionSource to track its progress.
// the Thread from the ThreadCompletionSource is added to the tasks lists to wait on.
await Task.Delay(500).ConfigureAwait(false);
TaskCompletionSource backThreadSource = new();
Thread instanceCaller = new(new ThreadStart(() =>
    DoubleLoop("Single Thread",
        1, 5,
        101, 105,
        backThreadSource)));
instanceCaller.Start();
tasks.Add(backThreadSource.Task);

await Task.WhenAll(tasks).ConfigureAwait(false);
```


This is a pretty simple tangent, and is not anything particularly
new. However, this is an important pattern to know, and it can
be utilized in many places in asynchronous code.

#### Navigation

[Full Sample](Samples/18TaskCompletionSourceSample.cs)

[Home](/)

[Previous: Standard async/await](17StdAwaitSample.md)

[Next: Constructing Cancellation Tokens](19MyCancellationTokenSample.md)

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

