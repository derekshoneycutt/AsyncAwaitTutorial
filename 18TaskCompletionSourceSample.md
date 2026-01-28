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

[Previous](17StdAwaitSample.md)

[Next](19MyCancellationTokenSample.md)
