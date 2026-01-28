### 15. Simulate async/await with Iterators

Now that we have actual asynchronous code, we want to work towards async/await
and the nice, readable code that it creates. First, we can use what we know about
iterator methods to yield return every Task we want to "await" on.
Then we can have an Iterate method that iterates and chains each task through 
ContinueWith. This Iterate is very similar to the refactored Consume we
just created, so we cna start with that and then refactor Consume once again.

First, we add the Iterate method. This takes an `IEnumerable<MyTask>` and
chains them together via ContinueWith. Specifically, this should MoveNext
and then add another MoveNext call via ContinueWith to the current Task if
there is still more in the collection.

```csharp
MyTask Iterate(IEnumerable<MyTask> tasks)
{
    MyTask task = new();

    IEnumerator<MyTask> enumerator = tasks.GetEnumerator();

    void MoveNext()
    {
        try
        {
            if (enumerator.MoveNext())
            {
                enumerator.Current.ContinueWith(MoveNext);
                return;
            }
        }
        catch (Exception ex)
        {
            task.SetException(ex);
            return;
        }

        task.SetResult();
    }

    MoveNext();

    return task;
}
```

Now we are ready to refactor the Consume method. Here, we just go back
to the simple foreach loop, but we now yield return each task that
we iterate over. This will simulate the "await" for us properly.

```csharp
IEnumerable<MyTask> Consume(
    string identifier,
    IEnumerable<MyTask<int>> values)
{
    // We update this to yield return each task we want to "await" on
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    foreach (MyTask<int> value in values)
    {
        yield return value;
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value.Result}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

Finally, we need to wrap our call to Consume with Iterate to wrap
it into a single MyTask.

```csharp
tasks.Add(Iterate(Consume(identifier, values)));
```

We now have something extremely close to async/await. In fact, we know
that the C# compiler compiles async/await and iterator methods extremely
similarly. They are both compiled into state machines that iterate
in this kind of way. It also looks like much nicer code now!

#### Navigation

[Full Sample](Samples/15IterateTaskGeneratorSample.cs)

[Home](/)

[Previous: Creating an Asynchronous Chain with ContinueWith](14MyTaskAsyncChainSample.md)

[Next: Using actual async/await with MyTask](16AwaitableCustomSample.md)

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


