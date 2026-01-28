### 25. Standard Channels

Now, take out our custom `MyChannel<T>` class and just use the standard channels.

Our Consumer remains perfect, we don’t need to change it. However, our Producer now needs to take in a standard
`ChannelWriter<T>` and use the WriteAsync method it provides.

```csharp
async Task Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    ChannelWriter<int> channel,
    CancellationToken cancellationToken)
{
    // Update to the standard channel writer
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        await channel.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        await channel.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }
}
```

We then just update our top level statement to use CreateUnbounded. We could also create Bounded,
PrioritizedUnbounded, and play with the many options that are available in the standard channels,
but we will just keep it simple and use Unbounded for now. Note that this has Reader and Writer
properties that must be referenced instead of the `Channel<T>` directly for all operations, so we make that change as well.

```csharp
 Channel<int> channel = Channel.CreateUnbounded<int>();

for (int index = 1; index <= Environment.ProcessorCount * 2; ++index)
{
    string identifier = $"Action {index}";
    _ = Consume(identifier, channel.Reader.ReadAllAsync(cancellationToken), cancellationToken);
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
channel.Writer.Complete();

await Task.Delay(500, cancellationToken).ConfigureAwait(false);

Console.WriteLine("All fin");
```

And this completes our work with channels! We now are effectively using the Producer/Consumer pattern with highly
async code and supporting multiple producers and multiple consumers on the same channel.

#### Navigation

[Full Sample](Samples/25ChannelsSample.cs)

[Home](/)

[Previous](24MyChannelSample.md)

[Next](26StructuredChannelsSample.md)
