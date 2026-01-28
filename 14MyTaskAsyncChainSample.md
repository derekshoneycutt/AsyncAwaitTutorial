### 14. Creating an Asynchronous Chain with ContinueWith

It is time for the first truly asynchronous take on our code. The way
we are going to do this will be somewhat ugly and reminiscent of the
state machine code we had before, breaking a loop into redundant calls.
However, this will give us truly asynchronous code, and it will give us
important insights into how async/await works.

First, we need to upgrade our ContinueWith method and add a couple of
additional ones that allow us to do long chains of continuations.
First, we update our existing ContinueWith to return a Task that
completes when the continuation has completed.


```csharp
public MyTask ContinueWith(Action action)
{
    // Update to return a Task that completes once the continuation has completed
    MyTask task = new();

    lock (_synchronize)
    {
        SetContinuationUnprotected(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                task.SetException(ex);
                return;
            }

            task.SetResult();
        });
    }

    return task;
}
```

The `MyTask<TResult>` class also needs a new ContinueWith,
which should take a `Func<TResult>` that allows the
continuation function to handle the result of the first task.
We could easily think of a set of others to add here, including
synchronous continuations and an async continuation that returns
a value, possibly of a different type than TResult. We will do just
the one we actually need today, but you can easily imagine an expansion.

```csharp
public MyTask ContinueWith(Func<TResult> action)
{
    return ContinueWith(() => action(_result));
}
```

Finally, we have to update the Consume method. Produce is already
about as async as it can get with our current tools. However,
we need to completely rewrite Consume to iterate over the IEnumerable
it gets via ContinueWith instead of a foreach loop. This is
pseudo-recursive, in that it does not do true recursion, and yet
the ContinueWiths make it look very much so like it. It also
has a lot in common with a state machine. In the end, we return
a MyTask that completes when the whole IEnumerable has been iterated.

```csharp
public static MyTask Consume(
    string identifier,
    IEnumerable<MyTask<int>> values)
{
    MyTask task = new();

    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    IEnumerator<MyTask<int>> state = values.GetEnumerator();

    void MoveNext()
    {
        try
        {
            if (state.MoveNext())
            {
                state.Current.ContinueWith(value =>
                {
                    Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
                    MoveNext();
                });
                return;
            }
        }
        catch (Exception ex)
        {
            task.SetException(ex);
            return;
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
        task.SetResult();
    }

    MoveNext();

    return task;
}
```

Finally, our Run code simplifies because we no longer need to do the `MyTask.Run`
since Consume will return a task directly.

```csharp
List<MyTask> tasks = [];
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    // Just call Consume straight and add its returned task to the list
    IEnumerable<MyTask<int>> values = Produce(
        1 + mod.Value, 5 + mod.Value,
        1001 + mod.Value, 1005 + mod.Value);
    tasks.Add(Consume(identifier, values));
}

MyTask.WhenAll(tasks).Wait();
```

Running this, for the first time, we see the full behavior of asynchronous code.
Every iteration is immediately started on the main thread, but then quickly
offloaded to the thread pool. With every advance, each task jumps to the next
available thread in the pool, no longer sticking to a single thread.
This looks nowhere as pretty as async/await might, but we do have truly
asynchronous code here now.

#### Navigation

[Full Sample](Samples/14MyTaskAsyncChainSample.cs)

[Home](/)

[Previous](13MyTaskDelaySample.md)

[Next](15IterateTaskGeneratorSample.md)

