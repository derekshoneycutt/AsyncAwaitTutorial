### 21. Standard Cancellation Tokens

The basic next step is to remove the custom cancellation token source
and token struct. Replace all instances of MyCancellationToken
with just the standard CancellationToken.

When we do this, we can now also pass the canceled token in to
a TaskCompletionSource, providing even mroe information when things
happen in our code.

```csharp
void DoubleLoop(
    string identifier,
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    TaskCompletionSource completionSource,
    CancellationToken cancellationToken)
{
    // Replace the parameter with the standard cancellation token type
    try
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        for (int value = firstStart; value <= firstEnd; ++value)
        {
            Thread.Sleep(1000);
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            Thread.Sleep(1000);
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        cancellationToken.ThrowIfCancellationRequested();
        Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");

        completionSource.SetResult();
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        // With the standard cancellation token, we can send the canceled token into our task completion source for more information to the caller
        completionSource.SetCanceled(cancellationToken);
    }
    catch (Exception ex)
    {
        completionSource.SetException(ex);
    }
}
```

We can now also pass the standard token down to other calls,
such as Task.Delay.

```csharp
async Task<int> DelayOnNumber(
    int number,
    CancellationToken cancellationToken)
{
    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
    return number;
}
```

And our Run code now just uses the standard CancellationTokenSource.
Note that in the samples, each one is passed in a standard Token,
and so we can use this to create a Linked Token Source as well.
This is shown here for demonstration, but you can skip it if you
are following along.

```csharp
CancellationTokenSource cts = new();//new(4500);

CancellationTokenSource linked =
    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

await using CancellationTokenRegistration cancelRegister = linked.Token.Register(() =>
{
    Console.WriteLine("Registered cancellation.");
});

List<Task> tasks = [];
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    IEnumerable<Task<int>> values = Produce(
        1 + mod.Value, 5 + mod.Value,
        1001 + mod.Value, 1005 + mod.Value,
        cts.Token);
    tasks.Add(Consume(identifier, values, cts.Token));
}

//We can pass the cancellation token down now that we know what to do!
await Task.Delay(500, cancellationToken).ConfigureAwait(false);
TaskCompletionSource backThreadSource = new();
Thread instanceCaller = new(new ThreadStart(() =>
    DoubleLoop("Single Thread",
        1, 5,
        101, 105,
        backThreadSource, linked.Token)));
instanceCaller.Start();
tasks.Add(backThreadSource.Task);

try
{
    await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
    // the standard cancel has an async cancel method
    // note: this one can cause problems with the UI thread in WPF, etc., so can't
    //   always be used, but it is nice when we can!
    await cts.CancelAsync().ConfigureAwait(false);

    await Task.WhenAll(tasks).ConfigureAwait(false);

    Console.WriteLine("All fin");
}
catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
{
    Console.WriteLine("Canceled");
}
```

Finally, we are now appropriately armed for moving forward to clean
the most clumsy part of our code still remaining. We are well armed
with the standard tools of asynchronous code that handle single objects,
and we have our code nicely separated into something like Producer
and Consumer.

#### Navigation

[Full Sample](Samples/21CancellationTokenSample.cs)

[Home](/)

[Previous](20IAsyncDisposableSample.md)

[Next](22CustomAsyncEnumerableSample.md)
