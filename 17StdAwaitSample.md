### 17. Standard async/await

We now remove our custom task classes entirely, and use just the
standard Task classes. It will become immediately apparent that
we do not have all the tools we want for this yet, as Produce
will need more refactoring.

To begin, we create a DelayOnNumber method. This just delays for
a second before returning a given value. This will be an async
method, and the Task representing the operation is what our
new Produce will return each iteration.

While doing this, we want to also begin to appropriately use
the ConfigureAwait method on the standard Task object. By default,
every Task is set as if you called `ConfigureAwait(true)`. This
causes the current task to attempt to return to the same execution
context that it started on before awaiting on a called Task.
However, this can be a performance hit, and in the vast majority
of non-UI library code, you want to explicitly call `ConfigureAwait(false)`
when you await a Task, such that you can continue the rest of the
method on any thread, or any context. You may utilize the default
behavior to stay on the UI thread without using Dispatcher, which
makes this extremely useful in some cases, however.

```csharp
async Task<int> DelayOnNumber(
    int number)
{
    await Task.Delay(1000).ConfigureAwait(false);
    return number;
}
```

The updated Produce method now looks nicer, although the
DelayOnNumber thing is not our favorite.

```csharp
IEnumerable<Task<int>> Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd)
{
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        yield return DelayOnNumber(value);
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        yield return DelayOnNumber(value);
    }
}
```

Consume method simply changes to using all standard Tasks,
and we also add in `ConfigureAwait(false)` here.

```csharp
async Task Consume(
    string identifier,
    IEnumerable<Task<int>> values)
{
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    foreach (Task<int> valueTask in values)
    {
        int value = await valueTask.ConfigureAwait(false);
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

We also can use `ConfigureAwait(false)` in our Run code if we want.

```csharp
await Task.WhenAll(tasks).ConfigureAwait(false);
```

We now have standard async/await code, and we should understand
it pretty clearly through this work. We have several pieces to
improve on yet, but this is very strong code.

#### Navigation

[Full Sample](Samples/17StdAwaitSample.cs)

[Home](/)

[Previous](16AwaitableCustomSample.md)

[Next](18TaskCompletionSourceSample.md)
