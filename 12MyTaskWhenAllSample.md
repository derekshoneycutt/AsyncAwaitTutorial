### 12. Implementing ContinueWith and WhenAll

This Task structure works great, but if we want to perform some action when a Task
is complete, we are entirely left to call Wait and then perform an action after that.
This is not ideal, and we want to add a ContinueWith as the standard `Task` has. This
will perform an operation once the Task has completed. We should then refactor our
Wait method to utilize this better. Finally, with this, we have all we need to
implement the WhenAll and get rid of the loop at the end of our Run code.

To implement ContinueWith, we will need to add some additional state fields
to store an action that should be run at completion. This should run on the
appropriate execution context, so we will capture that context and run the
ContinueWith on the thread pool with that context.

First, we add a private readonly record struct to the class to store continuation
data. We will have a field of this type that stores the current continuation, if
set.

We also remove the reset event field for waiting, as we will reconstruct that with
ContinueWith.

```csharp
private readonly record struct RunContinuation(
    Action? Continuation,
    ExecutionContext? ExecutionContext);


private RunContinuation _continuation = new(null, null);

```

Then we can add a private Execute method to execution the continuation, if
there is any. This will run it on the thread pool with the caught context.

```csharp
private static void Execute(RunContinuation continuation)
{
    if (continuation.Continuation is null)
    {
        return;
    }

    ThreadPool.QueueUserWorkItem<RunContinuation>(continuation =>
    {
        if (continuation.ExecutionContext is null)
        {
            continuation.Continuation!();
        }
        else
        {
            ExecutionContext.Run(continuation.ExecutionContext, act => ((Action)act!).Invoke(), continuation.Continuation);
        }
    }, continuation, true);
}
```

Now, in the Complete method, remove the signal on the reset event and
call execute with the current continuation.

```csharp
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

        // Run the continuation on the thread pool, no more wait event to set *here*
        Execute(_continuation);
    }
}
```

Now, we can add our ContinueWith method. We will use a private
SetContinuationUnprotected method to actually set the continuation data,
as we can avoid deadlocks when we refactor Wait this way.
If the Task is already complete at this point, we just immediately
execute the action on the captured context. Otherwise, we set
the continuation and let it be run upon completion.

```csharp
private void SetContinuationUnprotected(Action action)
{
    RunContinuation continuation = new(action, ExecutionContext.Capture());
    if (_completed)
    {
        Execute(continuation);
    }
    else
    {
        _continuation = continuation;
    }
}

public void ContinueWith(Action action)
{
    lock (_synchronize)
    {
        SetContinuationUnprotected(action);
    }
}
```


Finally, we will refactor Wait. If the Task is already completed,
we will just return immediately. Otherwise we will create a
manual reset event and set the Task's continuation to the
reset event's Set method. We can then wait on the reset event.

```csharp
public void Wait()
{
    // Refactor the Wait method to use the continuation to set a reset event created here.

    ManualResetEventSlim? reset = null;

    lock (_synchronize)
    {
        if (!_completed)
        {
            reset = new();
            SetContinuationUnprotected(reset.Set);
        }
    }

    reset?.Wait();

    if (_exception is not null)
    {
        ExceptionDispatchInfo.Throw(_exception);
    }
}
```

Finally, we can use the ContinueWith method we just created
to create an effective WhenAll that is not just a loop but actually
is represented as a Task that can be waited on, itself.

We start our WhenAll by creating a Task that will complete when
all of the tasks passed in are completed. We just need to create that logic.
We will have a counter set to the number of tasks that are
passed in to our WhenAll. Then we call ContinueWith on each of the
tasks with a continuation that decrements the counter. If the counter
hits 0, we set the WhenAll task as complete.

```csharp
public static MyTask WhenAll(params IEnumerable<MyTask> tasks)
{
    MyTask task = new();

    List<MyTask> useTasks = [.. tasks];
    if (useTasks.Count < 1)
    {
        task.SetResult();
    }
    else
    {
        int remaining = useTasks.Count;

        void Continuation()
        {
            if (Interlocked.Decrement(ref remaining) < 1)
            {
                task.SetResult();
            }
        }

        foreach (MyTask useTask in useTasks)
        {
            useTask.ContinueWith(Continuation);
        }
    }

    return task;
}
```

Finally, we just replace the last loop in our Run code with
a single line call to this new WhenAll.

```csharp
MyTask.WhenAll(tasks).Wait();
```

This step did an awful lot of work for what seems very little
gain so far. However, having the ability to do a ContinueWith
truly enables us to begin venturing into asynchronous code,
and out of simple concurrency.

#### Navigation

[Full Sample](Samples/12MyTaskWhenAllSample.cs)

[Home](/)

[Previous: Custom Task Class](11MyTaskSample.md)

[Next: Implementing Delay and Task&lt;TResult&gt;](13MyTaskDelaySample.md)

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

