### 23. IAsyncEnumerable Iterator Methods

Just as we replaced the IEnumerable implementation with an IEnumerable and using yield return,
we do the exact same thing here. We instead return IAsyncEnumerable, and we mark the
method as async, using await directly in it. That is, we can use yield return and await
together in this. Knowing how it is compiled helps us understand how it works.

We should certainly also take in a CancellationToken in this method. When doing so,
we now add the EnumeratorCancellation attribute on said parameter, allowing the
compiler to take additional optimizations for us.


```csharp
async IAsyncEnumerable<int> Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        yield return value;
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        yield return value;
    }
}
```

Our Consume already handles this fine from the previous step, so we just update the top level to call this new Produce and we see the familiar output.

```csharp
IAsyncEnumerable<int> values = Produce(
    1 + mod.Value, 5 + mod.Value,
    1001 + mod.Value, 1005 + mod.Value,
    cancellationToken);
tasks.Add(Consume(identifier, values, cancellationToken));
```
We now just have a nice 2 methods for our Produce/Consume setup once again, and the output remains the same as it has the entire time so far.
You can delete the custom IAsyncEnumerator implementation at this point as we will no longer even refer to it, preferring to simply use the
iterator methods like this instead moving forward.

The only problem with what we have now is that we are still tied to a single producer and a single consumer.
We cannot support multiple producers on one channel or multiple consumers on one channel. This is explicitly still only one-to-one.

#### Navigation

[Full Sample](Samples/23IAsyncEnumerableIteratorSample.cs)

[Home](/)

[Previous](22CustomAsyncEnumerableSample.md)

[Next](24MyChannelSample.md)

