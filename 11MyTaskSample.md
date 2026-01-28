### 11. Custom Task Class

We have now returned a lot of functionality back via a basic Task Completion object,
but in asynchronous code we do not typically do this entire tracking in
every single method. Rather, the compiler and Task library does this for us
most of the time. We want to take this TaskCompletion and add a method to Run
an action and track its completion.

For this, we will rename the `MyTaskCompletion` to just `MyTask` as we
are now trying to construct a more full Task class.

We add a new readonly record struct that we will use to pass task state to the
operation on the thread pool. This is basically the action to run and the `MyTask`
object used to track completion.

```csharp
private readonly record struct RunTask(
    Action Action,
    MyTask Task);
```

We then add a static `MyTask.Run` method that takes an `Action`, and runs
it on the thread pool. This should track for when that Action completes,
utilizing a new `MyTask` that is returned in this method.

```csharp
public static MyTask Run(Action action)
{
    MyTask task = new();

    ThreadPool.QueueUserWorkItem<RunTask>(task =>
    {
        try
        {
            task.Action();
        }
        catch (Exception ex)
        {
            task.Task.SetException(ex);
            return;
        }

        task.Task.SetResult();
    }, new(action, task), true);

    return task;
}
```

Now, we no longer need the `ThreadPoolState` record, so we remove that,
and we update the `Consume` method to just be normal, not taking a `MyTask`
any more, and not tracking its own progress any more.

```csharp
void Consume(
    string identifier,
    IEnumerable<int> values)
{
    // Remove all the funny tracking we had to add before! We're back to just a normal looking method!
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    foreach (int value in values)
    {
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

Finally, we update our Run code to use the new `MyTask.Run` to launch our
Consume methods.

```csharp
List<MyTask> tasks = [];
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    // Now use MyTask.Run to run the simpler method and track it the same!
    tasks.Add(MyTask.Run(() =>
    {
        IEnumerable<int> values = Produce(
            1 + mod.Value, 5 + mod.Value,
            1001 + mod.Value, 1005 + mod.Value);
        Consume(identifier, values);
    }));
}

foreach (MyTask task in tasks)
{
    task.Wait();
}
```

This should start to be looking a lot more familiar to the C# developer who has
used async/await and the Task library before. We are still technically not operating
asynchronously, and in fact we are exhausting the thread pool in the process.


#### Navigation

[Full Sample](Samples/11MyTaskSample.cs)

[Home](/)

[Previous: Custom Task Completion Class](10MyTaskCompletionSample.md)

[Next: Implementing ContinueWith and WhenAll](12MyTaskWhenAllSample.md)

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
