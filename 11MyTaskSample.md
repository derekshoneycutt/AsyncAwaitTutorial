### 11. Custom Task Class

We have now returned a lot of functionality back via a basic Task Completion object,
but in asynchronous code we do not typically do this entire tracking in
every single method. Rather, the compiler and Task library does this for us
most of the time. We want to take this TaskCompletion and add a method to Run
an action and track its completion.

For this, we will rename the `MyTaskCompletion` to just `MyTask` as we
are now trying to construct a more full Task class.

We add a new readonly record struct that we will use to pass task state to the
operation on the thread pool. This is basically the action to run and the `MyTask`
object used to track completion.

```csharp
private readonly record struct RunTask(
    Action Action,
    MyTask Task);
```

We then add a static `MyTask.Run` method that takes an `Action`, and runs
it on the thread pool. This should track for when that Action completes,
utilizing a new `MyTask` that is returned in this method.

```csharp
public static MyTask Run(Action action)
{
    MyTask task = new();

    ThreadPool.QueueUserWorkItem<RunTask>(task =>
    {
        try
        {
            task.Action();
        }
        catch (Exception ex)
        {
            task.Task.SetException(ex);
            return;
        }

        task.Task.SetResult();
    }, new(action, task), true);

    return task;
}
```

Now, we no longer need the `ThreadPoolState` record, so we remove that,
and we update the `Consume` method to just be normal, not taking a `MyTask`
any more, and not tracking its own progress any more.

```csharp
void Consume(
    string identifier,
    IEnumerable<int> values)
{
    // Remove all the funny tracking we had to add before! We're back to just a normal looking method!
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    foreach (int value in values)
    {
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

Finally, we update our Run code to use the new `MyTask.Run` to launch our
Consume methods.

```csharp
List<MyTask> tasks = [];
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    // Now use MyTask.Run to run the simpler method and track it the same!
    tasks.Add(MyTask.Run(() =>
    {
        IEnumerable<int> values = Produce(
            1 + mod.Value, 5 + mod.Value,
            1001 + mod.Value, 1005 + mod.Value);
        Consume(identifier, values);
    }));
}

foreach (MyTask task in tasks)
{
    task.Wait();
}
```

This should start to be looking a lot more familiar to the C# developer who has
used async/await and the Task library before. We are still technically not operating
asynchronously, and in fact we are exhausting the thread pool in the process.


#### Navigation

[Full Sample](Samples/11MyTaskSample.cs)

[Home](/)

[Previous](10MyTaskCompletionSample.md)

[Next](12MyTaskWhenAllSample.md)