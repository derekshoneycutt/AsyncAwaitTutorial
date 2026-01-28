### 13. Implementing Delay and Task&lt;TResult&gt;

As we want to move to actual asynchronous code, we first add in
a Task.Delay implementation so we can get off of Thread.Sleep.
Once this is complete, we can start returning Tasks from our
Producer as well, though we need to add a new `Task<TResult>`
class to handle Tasks with Return values.

First, the Delay is remarkably simple. We can just use a `Timer`
and set a Task as completed when the Timer runs out.

```csharp
public static MyTask Delay(int timeout)
{
    MyTask task = new();
    new Timer(_ => task.SetResult()).Change(timeout, -1);
    return task;
}
```

We then replace both Thread.Sleep calls with calls to this,
and call Wait on the returned Task.

```csharp
IEnumerable<int> Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd)
{
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        MyTask.Delay(1000).Wait();
        yield return value;
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        MyTask.Delay(1000).Wait();
        yield return value;
    }
}
```

This would be a very simple step if that is all we did, so we
will also create a `MyTask<TResult>` now. First, however, we make
the SetResult method of the normal MyTask virtual, and make the
private Complete method protected. Next, we create a new class
that inherits the original `MyTask` and takes 1 generic parameter.

The standard `Task<TResult>` is far more capable than we are going to
try to accomplish, but basically, we just need a Result property
that will wait for completion before returning, a method
to set the result with a Result value, and a ContinueWith method that
also passes the result value. This will need a field to
store the result. As a final precaution, we will have the
parameterless SetResult throw an exception if called.

```csharp
public class MyTask<TResult>
    : MyTask
{
    private TResult _result = default!;

    public TResult Result
    {
        get
        {
            Wait();
            return _result;
        }
    }

    public override void SetResult()
    {
        throw new InvalidOperationException();
    }

    public void SetResult(TResult value)
    {
        if (!IsCompleted)
        {
            _result = value;
            Complete(null);
        }
    }
}
```

Next, we update our producer to return `IEnumerable<MyTask<int>>`.
In each loop iteration, we then make a new `MyTask<int>`
and set the result to the value in the ContinueWith of the prior delay.
We no longer use Wait in the Produce then, instead doing `yield return`
with each of the `MyTask<int>` that we create.

```csharp
IEnumerable<MyTask<int>> Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd)
{
    // Update all of this to return MyTask<int> that finishes with the value after the delay.
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        MyTask<int> returnTask = new();
        MyTask.Delay(1000).ContinueWith(() => returnTask.SetResult(value));
        yield return returnTask;
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        MyTask<int> returnTask = new();
        MyTask.Delay(1000).ContinueWith(() => returnTask.SetResult(value));
        yield return returnTask;
    }
}
```


Our Consume method now also needs to update to take these on
each iteration. It also needs to print the Result property.
Note what this does. Technically, the Wait is happening in the Consume
more explicitly than the Produce now. This is an interesting fact,
and it exposes that simply decoupling with iterator methods actually
leaves us far more coupled than it immediately appears.

```csharp
void Consume(
    string identifier,
    IEnumerable<MyTask<int>> values)
{
    // Remove all the funny tracking we had to add before! We're back to just a normal looking method!
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    foreach (MyTask<int> value in values)
    {
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value.Result}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

Finally, just one line in the Run code gets this all working.

```csharp
List<MyTask> tasks = [];
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    tasks.Add(MyTask.Run(() =>
    {
        // Update to MyTask<int> values
        IEnumerable<MyTask<int>> values = Produce(
            1 + mod.Value, 5 + mod.Value,
            1001 + mod.Value, 1005 + mod.Value);
        Consume(identifier, values);
    }));
}

MyTask.WhenAll(tasks).Wait();
```

Running this, sometimes the code appears to be able to go even more
parallel than some prior steps have gotten us. The code is starting
to feel oddly asynchronous as well, although we are mostly just still
running with concurrency. This is still a great step forward, and
we can now start considering actual asynchrony.

#### Navigation

[Full Sample](Samples/13MyTaskDelaySample.cs)

[Home](/)

[Previous](12MyTaskWhenAllSample.md)

[Next](14MyTaskAsyncChainSample.md)

