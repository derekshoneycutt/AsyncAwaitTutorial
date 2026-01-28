### 26. Structuring a Channels Pipeline

Now, if we move the code around a bit, we can have a more structured setup that allows easy extension of a robust data pipeline.
The basic pattern here is used in a lot of C# code utilizing Channels for communications. 

For this, first we will create a basic Producer class. This will house the Channel as a private field and offer a ReadAllAsync
method routing to the ChannelReader ReadAllAsync. It will also have a basic async method to run a bunch of producers.
In short, it will look something like this:

```csharp
class Producer(int count)
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();
    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
 
    public async Task Run(CancellationToken cancellationToken)
    {
    }
}
```

To fill this in, we will move our Produce method inside our Producer method as a private method.
For this, we will reference the private _channel field to produce values onto. We can also pull
the top-level statements for running producers and pull them into the Run with slight modifications after that.
In this sample, I also increase the Delay time to a full second for better showing.

```csharp
private async Task Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    CancellationToken cancellationToken)
{
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        await _channel.Writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        await _channel.Writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }
}

public async Task Run(CancellationToken cancellationToken)
{
    List<Task> productionTasks = [];
    for (int index = 0; index < count; ++index)
    {
        int mod = 10 * index;
        productionTasks.Add(Produce(
            1 + mod, 5 + mod,
            1001 + mod, 1005 + mod,
            cancellationToken));
    }
    await Task.WhenAll(productionTasks).ConfigureAwait(false);
    _channel.Writer.Complete();
}
```

Now, we want something similar for our consumer. We make a Consumer class, containing our Consume method
as a private method and a Run method that contains the logic to run the consumers, as we have previously
in the top-level statements. In this code, I use the same pattern as the Run of Producer to await all consumer tasks as well.

```csharp
class Consumer
{
    private async Task Consume(
        string identifier,
        IAsyncEnumerable<int> values,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
    }

    public async Task Run(
        IAsyncEnumerable<int> values,
        CancellationToken cancellationToken)
    {
        List<Task> consumers = [];
        for (int index = 1; index <= Environment.ProcessorCount * 2; ++index)
        {
            string identifier = $"Action {index}";
            consumers.Add(Consume(identifier, values, cancellationToken));
        }
        await Task.WhenAll(consumers).ConfigureAwait(false);
    }
}
```

Finally, we fill in the top level statements to create a Producer and a Consumer and run them asynchronously.

```csharp
Producer producer = new(55);
Consumer consumer = new();
_ = consumer.Run(producer.ReadAllAsync(cancellationToken), cancellationToken);
        
List<Task> tasks = [producer.Run(cancellationToken)];

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

await Task.Delay(500, cancellationToken).ConfigureAwait(false);

Console.WriteLine("All fin");
```

This will give us the same sort of output as the previous steps,
but now our code is well organized into 2 classes, and this decoupling can allow us to extend it further with some clarity.

#### Navigation

[Full Sample](Samples/26StructuredChannelsSample.cs)

[Home](/)

[Previous](25ChannelsSample.md)

[Next](27ChannelMiddlemanSample.md)
