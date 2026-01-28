### 4. Use IEnumerable/IEnumerator Interfaces

In the last step, the state machine that we created closely resembles the Iterator
pattern. In fact, since we are looping through 2 ranges of values one after the other,
the Iterator pattern is a good fit for what we are doing. By continuing our implementation
of this pattern to the standard interfaces for the pattern, we can take advantage
of some of the language features of C# to make more maintainable code. We will go
ahead and extend to that interface in this step.

First, we add the `IEnumerator<int>` interface to our `Producer` class.
We have a few extremely easy bits to add to fulfill the interface, but they are all
fairly obvious. We do not have anything to clean up in Dispose, so we do not really
implement it beyond what Visual Studio suggests.


```csharp
class Producer(int firstStart, int firstEnd, int secondStart, int secondEnd)
    : IEnumerator<int>
{
    object IEnumerator.Current => Current;
 
    public void Reset()
    {
        _position = Position.Initial;
        Current = -1;
    }
 
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }


```

Then we add an `IEnumerable<int>` implementation that will just create a new
`Producer` class when `GetEnumerator` is called.

```csharp
class ProductionEnumerable(int firstStart, int firstEnd, int secondStart, int secondEnd)
    : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator()
    {
        return new Producer(firstStart, firstEnd, secondStart, secondEnd);
    }
 
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

```

Then we just update Consume to take an `IEnumerable<int>` and use a
foreach again instead of the current while. This is basically the same as we had
after step 2 again.

```csharp
void Consume(
    string identifier,
    IEnumerable<int> values)
{
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");
 
    foreach (int value in values)
    {
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
    }
 
    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

Finally, update the main code to create the new `ProductionEnumerable` and pass that into Consume.

```csharp
ProductionEnumerable producer = new(
    1 + mod, 5 + mod,
    1001 + mod, 1005 + mod);
Consume(identifier, producer);
```

Again, this will have identical output to our previous concerns, but the foreach in the `Consume`
looks a lot nicer. We now have quite a nice Consumer, although our Producer remains unwieldy.
Nonetheless, our Producer does follow the common iterator pattern with standard interfaces, which
allows us to use the features of the language built around this effectively.

#### Navigation

[Full Sample](Samples/04IEnumerableProducerSample.cs)

[Home](/)

[Previous](03StateMachineProducerSample.md)

[Next](05IteratorProducerSample.md)