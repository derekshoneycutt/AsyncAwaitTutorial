### 9. Use the Standard Thread Pool

Now we know enough about how the Thread Pool works, we can just switch over to the
standard ThreadPool, which includes several optimizations and makes our code cleaner
due to not having the ThreadPool around on us.

Simply, we can just comment out or delete the MyThreadPool class. Then use
`ThreadPool.QueueUserWorkItem`. Note, this requires a state variable parameter in
the queued action, which we did not add in ours. Lets utilize this to optimize
our thread, however. We will create a readonly record struct `ThreadPoolState`
that takes a String identifier and a async local int Mod value.

```csharp
readonly record struct ThreadPoolState(string Identifier, AsyncLocal<int> Mod);
```

Then, instead of using the captured lambda state, we pass a new instance of this
record into the QueueUserWorkItem, using the generic version of the method
for type safety.

```csharp
ThreadPool.QueueUserWorkItem<ThreadPoolState>(state =>
{
    IEnumerable<int> values = Produce(
        1 + state.Mod.Value, 5 + state.Mod.Value,
        1001 + state.Mod.Value, 1005 + state.Mod.Value);
    Consume(state.Identifier, values);
}, new(identifier, mod), true);
```

This is a very simple step after our work with a custom thread pool, but now
we are ready to begin building a Task structure that tracks work done on the
thread pool, instead of using our counter and reset event.

#### Navigation

[Full Sample](Samples/09ThreadPoolSample.cs)

[Home](/)

[Previous](08MyThreadPoolWithContextSample.md)

[Next](10MyTaskCompletionSample.md)
