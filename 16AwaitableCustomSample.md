### 16. Using actual async/await with MyTask

As a stop gap to going to fully standard Tasks and async/await, we can
make our MyTask class work with async/await! The async methods will have
to return the standard Task, but we can make it so we can await on MyTask.
Let's go!

In order to use async/await with our custom Task object, we need to add
a GetAwaiter method that returns a struct implementing INotifyCompletion.
We just take in an instance of the custom task in the primary constructor
and add the necessary properties (IsCompleted) and methods (GetAwaiter,
GetResults, OnCompleted).

We will only add this to the `MyTask<TResult>` type, although the
GetResult() could return void and just call Wait on the normal task
as well. We won't use it immediately, only using the typed result tasks
in this step, so we skip it for brevity.

```csharp
class MYTask<TResult>
{

	// ...

    public struct Awaiter(MyTask<TResult> task) : INotifyCompletion
    {
        public readonly bool IsCompleted => task.IsCompleted;

        public readonly Awaiter GetAwaiter() => this;

        public readonly TResult GetResult() => task.Result;

        public readonly void OnCompleted(Action continuation)
        {
            task.ContinueWith(continuation);
        }
    }

    public Awaiter GetAwaiter() => new(this);

	// ...

}
```

Now, we just refactor the Consume method to be async Task and await
on the tasks we get in our loop.

```csharp
async Task Consume(
    string identifier,
    IEnumerable<MyTask<int>> values)
{
    // We update this to be async/await with our custom task type!
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    foreach (MyTask<int> valueTask in values)
    {
        int value = await valueTask;
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

Finally, we make some updates to our main Run code as well,
so that it is property async. We can get rid of the old Iterate method
altogether now, as the compiler does it all for us.

```csharp
// We only work with Tasks in this method now
List<Task> tasks = [];
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    IEnumerable<MyTask<int>> values = Produce(
        1 + mod.Value, 5 + mod.Value,
        1001 + mod.Value, 1005 + mod.Value);
    // And we remove the wrapping call to Iterate, since we just get a full Task object now.
    tasks.Add(Consume(identifier, values));
}

// We can go ahead and await on the Task.WhenAll now, instead of Wait!
await Task.WhenAll(tasks);
```

This is a really cool point that we are now doing async/await, and
at this level of detail, we have significant insight into how it
works in multiple levels. The only thing left is to switch to
properly standard async/await throughout.

#### Navigation

[Full Sample](Samples/16AwaitableCustomSample.cs)

[Home](/)

[Previous: Simulate async/await with Iterators](15IterateTaskGeneratorSample.md)

[Next: Standard async/await](17StdAwaitSample.md)

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

