### 24. Custom Channels Implementation

In order to have multiple producers and multiple consumers on a single channel, we need to construct some kind of
new data structure that allows this kind of pattern. Although there is a standard structure, we’re going to first
create our own to show some basic concepts of how it works.

For this, we will create a generic class, let’s call it `MyChannel<T>`. We need 3 methods: Write, ReadAllAsync,
and Complete. Write will simply write a new message on the channel. ReadAllAsync will be an IAsyncEnumerable
iterator that yield returns each time a new message is available, and Complete will close down all functions in the channel.


```csharp
class MyChannel<T>
{
    public void Write(T value)
    {
 
    }
 
    public async IAsyncEnumerable<T> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
 
    }
 
    public void Complete()
    {
 
    }
}
```

Internally, we will need some kind of thread safe queue that handles the messages. We will use a `readonly ConcurrentDictionary<T>` field.
We also need a signal that a new message is available to read, so we will use a SemaphoreSlim field. Finally, we need a flag indicating
completion, so we will add a volatile Boolean field for that, ensuring the compiler doesn’t optimize it away.

```csharp
private readonly ConcurrentQueue<T> _queue = [];
 
private readonly SemaphoreSlim _signal = new(0);
 
private volatile bool _completed = false;
```

Now, for the write method, we should make sure we’re not writing to a completed channel,
but otherwise, just add the value to the queue and signal the semaphore.

```csharp
public void Write(T value)
{
    lock(_signal)
    {
        if (_completed)
        {
            throw new InvalidOperationException();
        }
 
        _queue.Enqueue(value);
        _signal.Release();
    }
}
```

Complete will be almost identical but will just set our completed flag to true and signal th semaphore.

```csharp
public void Complete()
{
    lock (_signal)
    {
        if (_completed)
        {
            throw new InvalidOperationException();
        }
 
        _completed = true;
        _signal.Release();
    }
}
```

Finally, the ReadAllAsync will just loop as long as the completed flag is false, wait on the semaphore,
and try to read the next value when the semaphore returns. If the channel is already completed when the
semaphore returns, just release the semaphore again to signal all consumers that it is complete and exit the loop.

```csharp
public async IAsyncEnumerable<T> ReadAllAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    while (!_completed)
    {
        await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (!_completed && _queue.TryDequeue(out T? value) && (value is not null))
        {
            yield return value;
        }
        else if (_completed)
        {
            _signal.Release();
        }
    }
}
```

That’s all! Now we need to update our Produce method to just be a normal async Task method,
take in an instance of the channel, and write to it instead of returning values.

```csharp
async Task Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    MyChannel<int> channel,
    CancellationToken cancellationToken)
{
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        channel.Write(value);
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        channel.Write(value);
    }
}
```

We don’t need to change Consume at all here, but we do need to change our top level structure
to create producers and consumers with Channels at heart. We will structure it so that we now
call our consumers before we call the producers, but with the Channel, it will all work out.
We also now have many consumers and many producers. I create 50 producers and the number of
CPU cores x2 number of consumers. At the end, we need to wait on all of our producer Tasks
together, and then complete the channel to ensure that the consumers reach their end as well.

```csharp
MyChannel<int> channel = new();

for (int index = 1; index <= Environment.ProcessorCount * 2; ++index)
{
    string identifier = $"Action {index}";
    _ = Consume(identifier, channel.ReadAllAsync(cancellationToken), cancellationToken);
}

List<Task> tasks = [];
for (int index = 1; index <= 55; ++index)
{
    int mod = 10 * index;
    string identifier = $"Action {index}";
    tasks.Add(Produce(
        1 + mod, 5 + mod,
        1001 + mod, 1005 + mod,
        channel,
        cancellationToken));
}

await Task.Delay(500, cancellationToken).ConfigureAwait(false);
TaskCompletionSource backThreadSource = new();
Thread instanceCaller = new(new ThreadStart(() =>
    DoubleLoop("Single Thread",
        1, 5,
        101, 105,
        backThreadSource, cancellationToken)));
instanceCaller.Start();
tasks.Add(backThreadSource.Task);

await Task.WhenAll(tasks).ConfigureAwait(false);
channel.Complete();

await Task.Delay(500, cancellationToken).ConfigureAwait(false);

Console.WriteLine("All fin");
```

We now have a little application that has 50 producers, each running through 2 loops asynchronously,
and a dynamic number of consumers, each just waiting on messages from the producers and printing them as they arrive.
This is great!

The next step is to swap in the standard channels.

#### Navigation

[Full Sample](Samples/24MyChannelSample.cs)

[Home](/)

[Previous](23IAsyncEnumerableIteratorSample.md)

[Next](25ChannelsSample.md)
