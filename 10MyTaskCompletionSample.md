### 10. Custom Task Completion Class

We can easily see one major disadvantage of our thread pool code in that we
have to use a counter and reset event logic in order to track when something
has been completed on the thread pool. Additionally, we do not have a good way
to handle exceptions were they to occur in our actions on this thread pool,
certainly no nice way to bubble them to the main thread we queued the actions from.

This is the logic for the basic `Task` object that C# provides. Lets create a basic
`TaskCompletion` that allows us to track when a piece of work is finished or
when it encounters an exception.

This class will need a boolean field indicating if the work is completed, a nullable
Exception field to store caught exceptions from the work, a reset event to provide
a Wait method, and a Lock to make sure everything is thread safe. We will need a
public property indicating if the task is complete yet, as a boolean. As for methods,
we need one to set the result of the task as finished, one to set the exception,
and one to wait. Since setting the result and setting an exception both complete the
task, these will go to a private Complete method, which sets the completed field
to true, sets the exception if included, and sets the reset event for any waiting.
Wait just waits on the reset event.

Special attention is paid to the `Wait` method to re-throw an exception that was
set. We use `ExceptionDispatcher` to maintain deep stack trace information.


```csharp
class MyTaskCompletion
{
    private readonly Lock _synchronize = new();
    private bool _completed = false;
    private Exception? _exception = null;
    private readonly ManualResetEventSlim _waitEvent = new(false);

    public bool IsCompleted
    {
        get
        {
            lock (_synchronize)
            {
                return _completed;
            }
        }
    }

    private void Complete(Exception? ex)
    {
        lock (_synchronize)
        {
            if (_completed)
            {
                throw new InvalidOperationException("Cannot complete an already completed task.");
            }

            _completed = true;
            _exception = ex;

            _waitEvent.Set();
        }
    }

    public void SetResult()
    {
        Complete(null);
    }

    public void SetException(Exception ex)
    {
        Complete(ex);
    }

    public void Wait()
    {
        _waitEvent.Wait();

        if (_exception is not null)
        {
            ExceptionDispatchInfo.Throw(_exception);
        }
    }
}
```

Next, we can delete the old `_actionCount` and `_resetEvent` globals,
and update Consume to take a `MyTaskCompletion` and report completion
via it. We do this by wrapping Consume code in a try...catch block
and using SetResult at the end inside the try block, or SetException
inside the catch block.


```csharp
void Consume(
    string identifier,
    IEnumerable<int> values,
    MyTaskCompletion taskCompletion) // New parameter to track the task's completion with
{
    //Wrap the whole worker method in a try block
    try
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        foreach (int value in values)
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");

        // set the task as complete
        taskCompletion.SetResult();
    }
    catch (Exception ex)
    {
        // set the task as complete, but with an error state
        taskCompletion.SetException(ex);
    }
}
```

Now, in the top level run code, we want to create a `List<MyTaskCompletion>`
and for each iteration we run on the thread pool, create a new 
`MyTaskCompletion`, send it into Consume, and add it to the list.
At the end, we then Wait on each Task, just like we Joined the prior threads.
I add a `MyTaskCompletion` property to the `ThreadPoolState` record
to maintain optimizations as well.

```csharp
readonly record struct ThreadPoolState(string Identifier, AsyncLocal<int> Mod, MyTaskCompletion TaskCompletion);
```

```csharp
// Create a list of the tasks to monitor
List<MyTaskCompletion> tasks = [];
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    // Create a task to send to the instance method to track the completion of the work and add it to the list
    MyTaskCompletion taskCompletion = new();
    ThreadPool.QueueUserWorkItem<ThreadPoolState>(state =>
    {
        IEnumerable<int> values = Produce(
            1 + state.Mod.Value, 5 + state.Mod.Value,
            1001 + state.Mod.Value, 1005 + state.Mod.Value);
        Consume(state.Identifier, values, state.TaskCompletion);
    }, new(identifier, mod, taskCompletion), true);
    tasks.Add(taskCompletion);
}

// Wait for all the tasks instead of the reset event
foreach (MyTaskCompletion task in tasks)
{
    task.Wait();
}
```

This part has been quite a lift to get to. The TaskCompletion class is
remarkably simple, although it introduces us to the Tasking pattern that we
will repeat again and again. In fact, this is very closely related to the
`TaskCompletionSource` that comes in .NET Core. We will evaluate that later
when we understand the Task structure a little better by expanding this to a full
`Task` like structure.

#### Navigation

[Full Sample](Samples/10MyTaskCompletionSample.cs)

[Home](/)

[Previous: Use the Standard Thread Pool](09ThreadPoolSample.md)

[Next: Custom Task Class](11MyTaskSample.md)

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
