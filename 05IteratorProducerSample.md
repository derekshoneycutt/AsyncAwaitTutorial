### 5. Use Iterator Methods

Those who have been around C# for a while are well aware of yield return and might just be thinking,
“Can’t we just use an iterator method instead of this whole IEnumerable structure?” The answer is yes.
We will go ahead and make the iterator method for this. The compiler will essentially make the same code
we just did for us. Note: keep the manual instance around as we will update it to async in the future!

Note: I would suggest commenting and saving the IEnumerable and IEnumerator implementations for future
use when we have async better established.

The iterator method should just take two ranges again and loop through them, performing a Sleep and then
yield return for each value. This is extremely close to the Produce method we had before, and we can just
update it to IEnumerable, yield return, and including the Thread.Sleep now if we have it around still.

```csharp
IEnumerable<int> Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd)
{
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        Thread.Sleep(500);
        yield return value;
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        Thread.Sleep(500);
        yield return value;
    }
}
```

Our Consume method already handles this perfectly fine, so we just need to call Produce on the top level
instead of making a ProductionEnumerator instance:

```csharp
IEnumerable<int> values = Produce(
    1 + mod, 5 + mod,
    1001 + mod, 1005 + mod);
Consume(identifier, values);
```

Again, this has the same output as before, but now we have 2 extremely clean methods. One method
produces values, and the other consumes and prints them to the screen. This is the basic pattern we want
to capitalize on in our code moving forward.

The one big drawback that we have now, however, is that we are not asynchronous. Everything in this code
all happens on the same thread, in series. We want to make some parallelism possible! Unfortunately,
we do not even having threading explored in this tutorial yet, so we have some ways to go.

#### Navigation

[Full Sample](Samples/05IteratorProducerSample.cs)

[Home](/)

[Previous](04IEnumerableProducerSample.md)

[Next](06ThreadSample.md)
