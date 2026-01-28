### 8. Handle Thread Local Storage and Execution Contexts

Before we continue, our ThreadPool suffers some significant weaknesses
that are important for understanding many cases in the asynchronous code. We
will now try to improve our thread pool and demonstrate these issues and how
to overcome them.

First, we update our thread count to something a bit more realistic. We will
use `Environment.ProcessorCount` so that there is a thread for each core of
our processor.

```csharp
private static readonly int _threadCount = Environment.ProcessorCount;
```

We can then also increase the number of actions that we run in total, to say 55.
I also increase the Thread.Sleep interval to a full second, although this
is entirely optional.

```csharp
_actionCount = 55;
for (int index = 1; index <= 55; ++index)
```

This should work predictably, but we can expose an issue by utilizing thread
local storage. Instead of an `int mod = 10 * index` every iteration, we can utilize
a single `AsyncLocal<int> mod` and set it for each iteration. We then use `mod.Value`
each time we use the mod value. This forces us to use thread local storage.

```csharp
_actionCount = 55;
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    MyThreadPool.QueueUserWorkItem(() =>
    {
        IEnumerable<int> values = Produce(
            1 + mod.Value, 5 + mod.Value,
            1001 + mod.Value, 1005 + mod.Value);
        Consume(identifier, values);
    });
}

_resetEvent.Wait(cancellationToken);
```

However, when we do this and run the application, we find that every single iteration is
treated as if mod was just 10. They are not getting the 10 * index value that we expect!
To fix this, we must add support for execution contexts in our thread pool.

Importantly, this is also used in GUI programming significantly. By forcing actions to
run on the execution context from which they were called, we can force tasks added to
our thread pool to run on the Display thread when needed. We will see how we can take
advantage of this in asynchronous code later, but for now we just need to add the
support for execution contexts to our thread pool.

In our queue, instead of just taking an `Action` we also need to include the associated
`ExecutionContext`, which might be `null`. When an action is added to the queue,
we then need to capture the Execution Context, and when it is run in a thread,
run the action with the execution context if it is not null. I add a private Execute
method that takes the action and execution context and runs it accordingly,
as we can reuse this logic later.

```csharp
static class MyThreadPool
{
    private static readonly int _threadCount = Environment.ProcessorCount;

    private static readonly BlockingCollection<(Action, ExecutionContext?)> _actionQueue = [];

    private static void Execute((Action, ExecutionContext?) queued)
    {
        (Action action, ExecutionContext? executionContext) = queued;
        if (executionContext is null)
        {
            action();
        }
        else
        {
            ExecutionContext.Run(executionContext, act => ((Action)act!).Invoke(), action);
        }
    }

    static MyThreadPool()
    {
        for (int i = 0; i < _threadCount; ++i)
        {
            new Thread(() =>
            {
                while (true)
                {
                    // Run on the execution context instead of invoking directly here!
                    Execute(_actionQueue.Take());
                }
            })
            { IsBackground = true }.Start();
        }
    }

    public static void QueueUserWorkItem(Action action)
    {
        _actionQueue.Add((action, ExecutionContext.Capture()));
    }
}
```

Now when we execute this code, we see the appropriate values printed to the screen,
as the actions are being run on the context that allows them to view the
thread local storage appropriately.

#### Navigation

[Full Sample](Samples/08MyThreadPoolWithContextSample.cs)

[Home](/)

[Previous](07MyThreadPoolSample.md)

[Next](09ThreadPoolSample.md)

