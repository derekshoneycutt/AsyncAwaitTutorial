### 7. Make a Custom Thread Pool

The first step to make our code asynchronous will be to start using a Thread Pool, which
will allow individual threads to be reused for multiple tasks. We will first update our
existing concurrent code to run on a custom thread pool so that we have a good understanding
of what is going on.

We start by creating a static class `MyThreadPool`. We will add a readonly int field for the
thread count, and another readonly `BlockingCollection<Action>` field that contains the
actions that are waiting to be performed on the thread pool. We make a static constructor
that initiates background threads according to the thread count field. Each thread should
loop infinitely and run the next action on the blocking collection, if any are available.
Finally, the class also has a static `QueueUserWorkItem` method that adds an `Action` to
the collection to be run on the first available thread.
Note: these should be background threads so that they are automatically killed upon
exiting the application; foreground threads may prevent the application shutting down.

I use 2 threads here even though we will launch 5 instances of our Consume method on the
thread pool. This demonstrates how the threads are reused for the next task at hand,
and how the behavior of our current implementation works.

```csharp
static class MyThreadPool
{
    private static readonly int _threadCount = 2;

    private static readonly BlockingCollection<Action> _actionQueue = [];

    static MyThreadPool()
    {
        // We just create the number of threads as Background threads so that they are killed when the application exits
        for (int i = 0; i < _threadCount; ++i)
        {
            new Thread(() =>
            {
                // each thread just loops and when it is available, gets the next action on the worker queue and runs it
                while (true)
                {
                    _actionQueue.Take().Invoke();
                }
            })
            { IsBackground = true }.Start();
        }
    }

    public static void QueueUserWorkItem(Action action)
    {
        _actionQueue.Add(action);
    }
}
```

Now, we want to run each Consume on this new structure instead of maintaining
a list of threads. However, we can no longer effectively wait on our tasks
to be complete by just joining the threads. We must instead create a counter
and use a `ManualResetEventSlim` to trigger when everything is done. This is clumsy,
and the clumsiness is a motivation for the Task structure we will look at later.

For now, we create 2 global fields for current action count and the reset event.

```csharp
int _actionCount = 0;

ManualResetEventSlim _resetEvent = new(false);
```

Then, at the very end of our Consume method, we decrement the action count
and if it results in a 0 value, set the event to trigger completion.

```csharp
void Consume(
	//...
	
    if (Interlocked.Decrement(ref _actionCount) < 1)
    {
        _resetEvent.Set();
    }
}
```

Finally, we can update our Run code to spawn each instance of Consume
onto this new thread pool and wait on the reset event at the end.

```csharp
// make sure we know how many times we need to decrement the global counter
_actionCount = 5;
for (int index = 0; index <= 5; ++index)
{
    int mod = 10 * index;
    string identifier = $"Action {index}";
    // Instead of starting our own thread, launch on the thread pool!
    MyThreadPool.QueueUserWorkItem(() =>
    {
        IEnumerable<int> values = Produce(
            1 + mod, 5 + mod,
            1001 + mod, 1005 + mod);
        Consume(identifier, values);
    });
}

// wait for the last thread to finish now.
_resetEvent.Wait(cancellationToken);
```

The result of this step is that we are now reusing threads on a custom
thread pool. We are not asynchronous yet, but we are now building the
foundations of asynchrony quite well. The weakness of running long running
operations on the thread pool is highlighted here as having only 2 threads
means almost immediate thread exhaustion, and our tasks are sitting around
waiting to run. However, we have quite a bit of work to solve this yet.

#### Navigation

[Full Sample](Samples/07MyThreadPoolSample.cs)

[Home](/)

[Previous](06ThreadSample.md)

[Next](08MyThreadPoolWithContextSample.md)

