### 20. Introducing IAsyncDisposable for CancellationTokenRegistration

The standard CancellationToken has many features we have not implemented
in our quick example. One of these includes a structure that allows us
to easily unregister callbacks that have been registered. This structure
implements the standard IDisposable interface, and it also provides
a good time to introduce the asynchronous IAsyncDisposable.

First, we add an Unregister method to our MyCancellationTokenSource.

```csharp
public void Unregister(Action callback)
{
    lock (_callbacks)
    {
        if (!_isCancellationRequested && _callbacks.Contains(callback))
        {
            _callbacks.Remove(callback);
        }
    }
}
```

Next, we will create a small struct called `MyCancellationTokenRegistration`.
This will take a `MyCancellationTokenSource` and a callback `Action` in
the primary constructor. It will implement both `IDisposable` and
`IAsyncDisposable`. We will not include the full dispose pattern here,
as that is well beyond needs of this tutorial.

This gives us 2 methods to implement: Dispose and DisposeAsync. DisposeAsync
returns a ValueTask, which can also be used in async methods much like
Task. However, this provides some optimizations in particular scenarios.
Most will not use ValueTask regularly, but IAsyncDisposable is a common case.

```csharp
public readonly struct MyCancellationTokenRegistration(
    MyCancellationTokenSource source, Action callback)
    : IDisposable, IAsyncDisposable
{
    public void Dispose()
    {
        source.Unregister(callback);
    }

    public async ValueTask DisposeAsync()
    {
        source.Unregister(callback);
    }
}
```

Now, we update our Register method to return one of these structures,
allowing callbacks to be unregistered via this interface.

```csharp
public MyCancellationTokenRegistration Register(Action callback)
{
    lock (_callbacks)
    {
        if (!_isCancellationRequested)
        {
            _callbacks.Add(callback);
            return new(this, callback);
        }
    }

    callback();
    return new(this, callback);
}
```


Now we can handle this in our Run method where we add a 
registration. For example, we can now use `await using` and have
DisposeAsync called appropriately.

```csharp
await using MyCancellationTokenRegistration cancelRegister = cts.Register(() =>
{
    Console.WriteLine("Registered cancellation.");
});
```

In addition, we can call Dispose or DisposeAsync anywhere we wish
to unregister at as well.

```csharp
await cancelRegister.DisposeAsync().ConfigureAwait(false);
```

This step is mostly expanded to introduce the concept of the
`IAsyncDisposable` interface. Knowing it, we can now move on to
the standard cancellation token comfortably.

#### Navigation

[Full Sample](Samples/20IAsyncDisposableSample.cs)

[Home](/)

[Previous](19MyCancellationTokenSample.md)

[Next](21CancellationTokenSample.md)

