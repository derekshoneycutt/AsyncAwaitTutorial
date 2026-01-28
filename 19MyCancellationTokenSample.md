### 19. Constructing Cancellation Tokens

A significant drawback of the code strategies we have introduced
so far is that we can easily spawn off work to be done on the
thread pool, but we cannot easily stop work once it is started.
In fact, the standard libraries provide a CancellationToken for
this purpose, and a great deal of optimization is done to make sure
they work well. The current version is even lighter than older versions,
and that works great for us. Instead of simply diving in, we will once
again construct our own version and understand what is going on
much better.

First, we need to construct our Cancellation Token Source.
Having a separate Token and Source allows us to limit the cancellation
to only the owner of the source. We then pass the Token to consumers
who need the notification of cancellation.

Both of these should have an IsCancellationRequested property, but only
the Source should have a Cancel method. They should both be able to Register
actions to perform on cancellation, and the Token needs a
ThrowIfCancellationRequested method. The Source should call any registered
callbacks when Cancel is called.

```csharp
public readonly struct MyCancellationToken(MyCancellationTokenSource source)
{
	public bool IsCancellationRequested => source.IsCancellationRequested;

	public void Register(Action callback) => source.Register(callback);

	public void ThrowIfCancellationRequested()
	{
		if (IsCancellationRequested)
		{
            throw new OperationCanceledException();
		}
	}
}

public class MyCancellationTokenSource
{
		private volatile bool _isCancellationRequested = false;

        private readonly List<Action> _callbacks = [];

        private readonly MyCancellationToken _token;

        public MyCancellationToken Token => _token;

        public bool IsCancellationRequested => _isCancellationRequested;

        public MyCancellationTokenSource()
        {
            _token = new(this);
        }

        public void Register(Action callback)
        {
            lock(_callbacks)
            {
                if (!_isCancellationRequested)
                {
                    _callbacks.Add(callback);
                    return;
                }
            }

            callback();
        }

        public void Cancel()
        {
            lock (_callbacks)
            {
                if (_isCancellationRequested)
                {
                    return;
                }

                _isCancellationRequested = true;
            }

            foreach (Action callback in _callbacks)
            {
                callback();
            }
        }
}
```

With these pretty basic structures, we can now accept
a MyCancellationTokenSource in each method. Since we are
not using the standard tokens, we will need to call
ThrowIfCancellationRequested more frequently in our first
pass through our code. Ordinarily, simply passing the
standard token to another method suffices, and we do not
need to call Throw often.

Additionally, we can now also call SetCanceled on our
TaskCompletionSource when we catch a cancellation request.
When we move to the standard tokens, we will even pass in
the token, but for now, this provides more information
when things happen in our code.

```csharp
void DoubleLoop(
    string identifier,
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    TaskCompletionSource completionSource,
    MyCancellationToken cancellationToken)
{
    // We add a cancellation token parameter and add a bunch of polls to the cancellation token to ensure that we end if the process is continuing

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

        Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");

        completionSource.SetResult();
    }
    // We can now also specifically catch OperationCanceledException and send a Canceled state to our task completion source!
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        completionSource.SetCanceled();
    }
    catch (Exception ex)
    {
        completionSource.SetException(ex);
    }
}

async Task<int> DelayOnNumber(
    int number,
    MyCancellationToken cancellationToken)
{
    // We add a cancellation token parameter and poll it

    cancellationToken.ThrowIfCancellationRequested();
    await Task.Delay(1000).ConfigureAwait(false);
    cancellationToken.ThrowIfCancellationRequested();
    return number;
}

IEnumerable<Task<int>> Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    MyCancellationToken cancellationToken)
{
    // We add a cancellation token parameter and pass it along

    for (int value = firstStart; value <= firstEnd; ++value)
    {
        yield return DelayOnNumber(value, cancellationToken);
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        yield return DelayOnNumber(value, cancellationToken);
    }
}

async Task Consume(
    string identifier,
    IEnumerable<Task<int>> values,
    MyCancellationToken cancellationToken)
{
    // We add a cancellation token parameter and add a poll to the cancellation token to ensure that we end if the process is continuing

    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    foreach (Task<int> valueTask in values)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int value = await valueTask.ConfigureAwait(false);
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

Finally, in our Run code, we need to create a new Source,
register a callback method, and pass the Token to all of
our methods to be handled. We can also force an earlier cancellation
to observe the behavior.

```csharp
// Create a cancellation token source
MyCancellationTokenSource cts = new();

// Add a callback to perform something when the cancellation token is cancelled
cts.Register(() =>
{
    Console.WriteLine("Registered cancellation.");
});

List<Task> tasks = [];
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    // Add the cancellation token to the parameters
    IEnumerable<Task<int>> values = Produce(
        1 + mod.Value, 5 + mod.Value,
        1001 + mod.Value, 1005 + mod.Value,
        cts.Token);
    tasks.Add(Consume(identifier, values, cts.Token));
}

await Task.Delay(500).ConfigureAwait(false);
TaskCompletionSource backThreadSource = new();
// Add the cancellation token to the parameters
Thread instanceCaller = new(new ThreadStart(() =>
    DoubleLoop("Single Thread",
        1, 5,
        101, 105,
        backThreadSource, cts.Token)));
instanceCaller.Start();
tasks.Add(backThreadSource.Task);

// Handle cancellation with try...catch (OperationCancelledException)
try
{
    // Force an early cancellation!
    await Task.Delay(3000).ConfigureAwait(false);
    cts.Cancel();

    await Task.WhenAll(tasks).ConfigureAwait(false);

    Console.WriteLine("All fin");
}
catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
{
    Console.WriteLine("Canceled");
}
```

This section has been long in terms of touching all the code.
However, the structure of the cancellation tokens is remarkably
simple. Armed with the basic idea of how simple they are, we can
convert to the standard Tokens and have a little cleaner code.

#### Navigation

[Full Sample](Samples/19MyCancellationTokenSample.cs)

[Home](/)

[Previous](18TaskCompletionSourceSample.md)

[Next](20IAsyncDisposableSample.md)
