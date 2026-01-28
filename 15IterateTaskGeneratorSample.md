### 15. Simulate async/await with Iterators

Now that we have actual asynchronous code, we want to work towards async/await
and the nice, readable code that it creates. First, we can use what we know about
iterator methods to yield return every Task we want to "await" on.
Then we can have an Iterate method that iterates and chains each task through 
ContinueWith. This Iterate is very similar to the refactored Consume we
just created, so we cna start with that and then refactor Consume once again.

First, we add the Iterate method. This takes an `IEnumerable<MyTask>` and
chains them together via ContinueWith. Specifically, this should MoveNext
and then add another MoveNext call via ContinueWith to the current Task if
there is still more in the collection.

```csharp
MyTask Iterate(IEnumerable<MyTask> tasks)
{
    MyTask task = new();

    IEnumerator<MyTask> enumerator = tasks.GetEnumerator();

    void MoveNext()
    {
        try
        {
            if (enumerator.MoveNext())
            {
                enumerator.Current.ContinueWith(MoveNext);
                return;
            }
        }
        catch (Exception ex)
        {
            task.SetException(ex);
            return;
        }

        task.SetResult();
    }

    MoveNext();

    return task;
}
```

Now we are ready to refactor the Consume method. Here, we just go back
to the simple foreach loop, but we now yield return each task that
we iterate over. This will simulate the "await" for us properly.

```csharp
IEnumerable<MyTask> Consume(
    string identifier,
    IEnumerable<MyTask<int>> values)
{
    // We update this to yield return each task we want to "await" on
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    foreach (MyTask<int> value in values)
    {
        yield return value;
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value.Result}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

Finally, we need to wrap our call to Consume with Iterate to wrap
it into a single MyTask.

```csharp
tasks.Add(Iterate(Consume(identifier, values)));
```

We now have something extremely close to async/await. In fact, we know
that the C# compiler compiles async/await and iterator methods extremely
similarly. They are both compiled into state machines that iterate
in this kind of way. It also looks like much nicer code now!

#### Navigation

[Full Sample](Samples/15IterateTaskGeneratorSample.cs)

[Home](/)

[Previous](14MyTaskAsyncChainSample.md)

[Next](16AwaitableCustomSample.md)

