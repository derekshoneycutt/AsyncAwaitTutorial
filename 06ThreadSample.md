### 6. Make it Multithreaded

The first step to now make this asynchronous is to use Threads. We will simply update our code so that
each call to Consume runs on its own thread. We will see the weakness of this approach and build
something better as we progress.

For this step, we create a `List<Thread>` and initialize it as empty. Then inside our Run loop,
we launch the `Consume` method in a new `Thread` that is added to the list. At the end, we then
add a second loop that Joins each thread, effectively waiting for each instance to complete.

```csharp
List<Thread> threads = []; // Store threads spun off here
for (int index = 1; index <= 5; ++index)
{
    int mod = 10 * index;
    string identifier = $"Action {index}";
    // Create and start a thread, adding it to the collection
    Thread thread = new(new ThreadStart(() =>
    {
        IEnumerable<int> values = Produce(
            1 + mod, 5 + mod,
            1001 + mod, 1005 + mod);
        Consume(identifier, values);
    }));
    thread.Start();
    threads.Add(thread);
}

// Join all the stored threads to the current before finishing.
foreach (Thread thread in threads)
{
    thread.Join();
}
```

This is the first time our output is significantly changed. Now we have multiple streams
of production being consumed at the same time, instead of one after the other.
However, while concurrency is nice, this is not truly asynchronous. We need to build the
patterns and concepts for asynchrony.

#### Navigation

[Full Sample](Samples/06ThreadSample.cs)

[Home](/)

[Previous](05IteratorProducerSample.md)

[Next](07MyThreadPoolSample.md)
