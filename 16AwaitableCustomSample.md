### 16. Using actual async/await with MyTask

As a stop gap to going to fully standard Tasks and async/await, we can
make our MyTask class work with async/await! The async methods will have
to return the standard Task, but we can make it so we can await on MyTask.
Let's go!

In order to use async/await with our custom Task object, we need to add
a GetAwaiter method that returns a struct implementing INotifyCompletion.
We just take in an instance of the custom task in the primary constructor
and add the necessary properties (IsCompleted) and methods (GetAwaiter,
GetResults, OnCompleted).

We will only add this to the `MyTask<TResult>` type, although the
GetResult() could return void and just call Wait on the normal task
as well. We won't use it immediately, only using the typed result tasks
in this step, so we skip it for brevity.

```csharp
class MYTask<TResult>
{

	// ...

    public struct Awaiter(MyTask<TResult> task) : INotifyCompletion
    {
        public readonly bool IsCompleted => task.IsCompleted;

        public readonly Awaiter GetAwaiter() => this;

        public readonly TResult GetResult() => task.Result;

        public readonly void OnCompleted(Action continuation)
        {
            task.ContinueWith(continuation);
        }
    }

    public Awaiter GetAwaiter() => new(this);

	// ...

}
```

Now, we just refactor the Consume method to be async Task and await
on the tasks we get in our loop.

```csharp
async Task Consume(
    string identifier,
    IEnumerable<MyTask<int>> values)
{
    // We update this to be async/await with our custom task type!
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    foreach (MyTask<int> valueTask in values)
    {
        int value = await valueTask;
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

Finally, we make some updates to our main Run code as well,
so that it is property async. We can get rid of the old Iterate method
altogether now, as the compiler does it all for us.

```csharp
// We only work with Tasks in this method now
List<Task> tasks = [];
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    IEnumerable<MyTask<int>> values = Produce(
        1 + mod.Value, 5 + mod.Value,
        1001 + mod.Value, 1005 + mod.Value);
    // And we remove the wrapping call to Iterate, since we just get a full Task object now.
    tasks.Add(Consume(identifier, values));
}

// We can go ahead and await on the Task.WhenAll now, instead of Wait!
await Task.WhenAll(tasks);
```

This is a really cool point that we are now doing async/await, and
at this level of detail, we have significant insight into how it
works in multiple levels. The only thing left is to switch to
properly standard async/await throughout.

#### Navigation

[Full Sample](Samples/16AwaitableCustomSample.cs)

[Home](/)

[Previous](15IterateTaskGeneratorSample.md)

[Next](17StdAwaitSample.md)
