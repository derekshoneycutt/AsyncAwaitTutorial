# Async/Await Tutorial Samples


The point of this project is to provide a series of samples that progressively build to
and upon the basic idea of async/await in C#. Each example essentially builds on the
example prior, and so comparing to previous samples is a good way to understand what step
is being taken, and possibly why.


Some basic familiarity with C# and Threads is assumed, although this could
probably be useful for someone with familiarity with other languages understanding how
async/await code works in C# as well, as well as the idea of Threads is grasped already.


## Tutorial

The rest of this readme will read as a tutorial to create the code in this project, step by step.

Although the code in this project is structured such that each step is an isolated sample,
we will instead act as if we are evolving each step in a single application. The goal of this
application will be to loop through 2 ranges and print them to the screen. Eventually, this will
get more complicated with multiple producers running the loops through 2 ranges, but we will get there
slowly, starting with procedural code, making it async, and continuing on to more advanced
asynchronous patterns. Every sample is built from copying the prior and adding to it in
specific ways to teach another topic in asynchronous programming.

Currently this can be seen as going through in a series of specific sections. Feel free to skip ahead
for topics with clear familiarity.

1. 1-5 : Creating a base project that separates production of values and display of those values; includes explaining iterators.
1. 6-9 : Making the existing project into a multithreaded application, including using a thread pool and working with thread local storage and execution contexts.
1. 10-13 : Creating the Task structure to track work done on the thread pool.
1. 14-17 : Making async/await happen.
1. 18-23 : Async utilities; TaskCompletionSource, CancellationTokens, IAsyncDisposable, IAsyncEnumerable.
1. 24-27 : Asynchronous Channels.

### 1. Simple Procedural Code

[Simple Procedural Code](./01. ProceduralSample.md)

First, we need to create some scaffolding. We create a new C# Console Application project,
and we can use top level statements in Program.cs.

Now, we need to create a method that will loop through 2 ranges and print the values to the screen.
This should be pretty straight forward and gives us our starting point.

In order to demonstrate that perhaps this is a process that could take some time--like a call
to a web server--we will throw in a couple of `Thread.Sleep` instances.

We will also take in an identifier string that will print with each value, and we will print the current
managed thread ID. These are not very important in this first step, but will be helpful to
understand what is going on under the hood later.

```csharp
void DoubleLoop(
    string identifier,
    int firstStart, int firstEnd, int secondStart, int secondEnd)
{
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    for (int value = firstStart; value <= firstEnd; ++value)
    {
        Thread.Sleep(500);
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        Thread.Sleep(500);
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

We then create a loop to call it 5 times, with a unique identifier for each call.
We use the index of the loop to name the identifier. Also, we create a mod value that
changes each iteration to loop through different ranges. This will just be `10 * index`.

```csharp
for (int index = 1; index <= 5; ++index)
{
    int mod = 10 * index;
    string identifier = $"Action {index}";
    DoubleLoop(identifier,
        1 + mod, 5 + mod,
        1001 + mod, 1005 + mod);
}

Console.WriteLine("All fin");
```

Running this sample now should just give us a predictable loop of values being
printed to the screen.

This is pretty simple and gets us a baseline function to modify for the new pattern.
It also is a good example of what we want to move away from. Every time that we
run this, we have to wait for it to complete. Furthermore, this single method both
produces values and displays them, whereas we would like to decouple that
functionality.

### 2. First Producer: Produce a List

To begin, we will show some basic language features that will be important for
our asynchronous code later. For now, it will help us to decouple our code and
get used to some basic patterns.

First on order is to split up our code so that we have a method that produces
the values to display, and another method to display them. This is a common separation
made in many graphical applications, so it should be pretty simple for us still.

We will replace our current DoubleLoop method with new Produce and Consume methods.
Let's start with Produce.

At this stage, Produce will just take in the 2 ranges and return a List containing
all of the specified values.

```csharp
List<int> Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd)
{
    List<int> values = [];

    for (int value = firstStart; value <= firstEnd; ++value)
    {
        values.Add(value);
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        values.Add(value);
    }

    return values;
}
```

Consume then simply takes in the collection--we will use `IEnumerable<int>` immediately
here as it will be more flexible for future updates--and loops through each value,
printing it to the screen. This is now only a single loop!

```csharp
public static void Consume(
    string identifier,
    IEnumerable<int> values)
{
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    foreach (int value in values)
    {
        Thread.Sleep(500);
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

We must also update the run code for this to call Produce and then Consume.

```csharp
for (int index = 1; index <= 5; ++index)
{
    int mod = 10 * index;
    string identifier = $"Action {index}";
    List<int> values = Produce(
        1 + mod, 5 + mod,
        1001 + mod, 1005 + mod);
    Consume(identifier, values);
}
```

This gives us a really basic Producer/Consumer pattern, and in a way that is quite common
in C#. This is great for making easier, more maintainable code. However, this version
has many limitations. Namely, we have to wait for all of the values to be produced and then
print them. We maintain a Sleep in our Consume, but this does not simulate the common
cases. For example, if the values come from a web API call, the delay would be in the Produce.
In this setup, we would have to wait for all such delays to complete before we print them.
We will need to make it so we can consume values as they are produced, and this simple method
does not do the job yet.

### 3. Making Producer Sleep: State Machine

One of the most common structures that enables us to consume values as they are produced is
the state machine. This is a broad kind of structure that is used throughout many
programming challenges. In fact, many modern language features, including in C#, ultimately
are compiled down into forms of state machines.

In short, a state machine is just a representation of computation that is always in
one of many recognized positions called states. Some state machines have several actions
that can result in advancing the computation from one state to another. The simplest state
machines may simply have a value containing the current state and a Next method to advance
to the next state. The common Iterator is a form of this simple state machine. We will
construct such a simple state machine to show how we can consume values as they are produced.

We start by creating a basic class, which we call MyState for now. We put 2 range fields in 
our primary constructor: the two ranges to loop through. We also have a privately mutable 
property Current and a method MoveNext that returns a Boolean indicating if Current contains
a valid value. Note if your following this tutorial: we can keep our Produce method around,
we will return to it later; the sample code maintains it in a previous sample so removes it.

```csharp
class Producer(int firstStart, int firstEnd, int secondStart, int secondEnd)
{
    public int Current { get; private set; } = -1;
 
    public bool MoveNext()
    {
        // What to do?
    }
}
```

Since MoveNext will not actually contain a real loop, we need to track where in our 2
“loops” we are to “move next” each time MoveNext is called. Inside the MyState class, we
create a private enum Position with values Initial, FirstLoop, SecondLoop, and End,
and create a private property starting at Initial.

```csharp
private enum Position
{
    Initial,
    FirstLoop,
    SecondLoop,
    End
}
 
private Position _position = Position.Initial;
```

Now, we just need to fill out MoveNext. We can use a switch on _position to know where
we are. For each loop, we create small methods that handle a single iteration of each loop
and move forward. It is in here that we will perform our Sleep from the original method.
In the case we reach the end, return false. If we are at the end when MoveNext is called,
throw an exception.

```csharp
public bool MoveNext()
{
    bool FirstLoop()
    {
        if (Current <= firstEnd)
        {
            Thread.Sleep(500);
            return true;
        }
 
        _position = Position.SecondLoop;
        Current = secondStart;
        return SecondLoop();
    }
 
    bool SecondLoop()
    {
        if (Current <= secondEnd)
        {
            Thread.Sleep(500);
            return true;
        }
 
        _position = Position.End;
        return false;
    }
 
    switch (_position)
    {
        case Position.Initial:
            Current = firstStart;
            _position = Position.FirstLoop;
            return FirstLoop();
 
        case Position.FirstLoop:
            ++Current;
            return FirstLoop();
 
        case Position.SecondLoop:
            ++Current;
            return SecondLoop();
 
        default:
            throw new InvalidOperationException();
    }
}
```

This completes the state machine for this step.
Now, we modify Consume to take the Producer object. We also change the loop to a while
loop that continues if MoveNext() returns true. The value to print on each iteration
will then be the Current property of the Producer. The producer does the sleep for us,
so we remove that from this method as well.

```csharp
void Consume(
    string identifier,
    Producer producer)
{
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");
 
    while (producer.MoveNext())
    {
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {producer.Current}");
    }
 
    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

Finally, we update our running code to create a Producer for each instance and pass it
into the Consume method. The result is identical output to what we had before, but we have
now created a pattern of separate production and consumption that allows consuming
as values are produced!

```csharp
for (int index = 1; index <= 5; ++index)
{
    int mod = 10 * index;
    string identifier = $"Action {index}";
    Producer producer = new(
        1 + mod, 5 + mod,
        1001 + mod, 1005 + mod);
    Consume(identifier, producer);
}
```

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

### 7. Make a Custom Thread Pool

The first step to make our code asynchronous will be to start using a Thread Pool, which
will allow individual threads to be reused for multiple tasks. We will first update our
existing concurrent code to run on a custom thread pool so that we have a good understanding
of what is going on.

We start by creating a static class `MyThreadPool`. We will add a readonly int field for the
thread count, and another readonly `BlockingCollection<Action>` field that contains the
actions that are waiting to be performed on the thread pool. We make a static constructor
that initiates background threads according to the thread count field. Each thread should
loop infinitely and run the next action on the blocking collection, if any are available.
Finally, the class also has a static `QueueUserWorkItem` method that adds an `Action` to
the collection to be run on the first available thread.
Note: these should be background threads so that they are automatically killed upon
exiting the application; foreground threads may prevent the application shutting down.

I use 2 threads here even though we will launch 5 instances of our Consume method on the
thread pool. This demonstrates how the threads are reused for the next task at hand,
and how the behavior of our current implementation works.

```csharp
static class MyThreadPool
{
    private static readonly int _threadCount = 2;

    private static readonly BlockingCollection<Action> _actionQueue = [];

    static MyThreadPool()
    {
        // We just create the number of threads as Background threads so that they are killed when the application exits
        for (int i = 0; i < _threadCount; ++i)
        {
            new Thread(() =>
            {
                // each thread just loops and when it is available, gets the next action on the worker queue and runs it
                while (true)
                {
                    _actionQueue.Take().Invoke();
                }
            })
            { IsBackground = true }.Start();
        }
    }

    public static void QueueUserWorkItem(Action action)
    {
        _actionQueue.Add(action);
    }
}
```

Now, we want to run each Consume on this new structure instead of maintaining
a list of threads. However, we can no longer effectively wait on our tasks
to be complete by just joining the threads. We must instead create a counter
and use a `ManualResetEventSlim` to trigger when everything is done. This is clumsy,
and the clumsiness is a motivation for the Task structure we will look at later.

For now, we create 2 global fields for current action count and the reset event.

```csharp
int _actionCount = 0;

ManualResetEventSlim _resetEvent = new(false);
```

Then, at the very end of our Consume method, we decrement the action count
and if it results in a 0 value, set the event to trigger completion.

```csharp
void Consume(
	//...
	
    if (Interlocked.Decrement(ref _actionCount) < 1)
    {
        _resetEvent.Set();
    }
}
```

Finally, we can update our Run code to spawn each instance of Consume
onto this new thread pool and wait on the reset event at the end.

```csharp
// make sure we know how many times we need to decrement the global counter
_actionCount = 5;
for (int index = 0; index <= 5; ++index)
{
    int mod = 10 * index;
    string identifier = $"Action {index}";
    // Instead of starting our own thread, launch on the thread pool!
    MyThreadPool.QueueUserWorkItem(() =>
    {
        IEnumerable<int> values = Produce(
            1 + mod, 5 + mod,
            1001 + mod, 1005 + mod);
        Consume(identifier, values);
    });
}

// wait for the last thread to finish now.
_resetEvent.Wait(cancellationToken);
```

The result of this step is that we are now reusing threads on a custom
thread pool. We are not asynchronous yet, but we are now building the
foundations of asynchrony quite well. The weakness of running long running
operations on the thread pool is highlighted here as having only 2 threads
means almost immediate thread exhaustion, and our tasks are sitting around
waiting to run. However, we have quite a bit of work to solve this yet.

### 8. Handle Thread Local Storage and Execution Contexts

Before we continue, our ThreadPool suffers some significant weaknesses
that are important for understanding many cases in the asynchronous code. We
will now try to improve our thread pool and demonstrate these issues and how
to overcome them.

First, we update our thread count to something a bit more realistic. We will
use `Environment.ProcessorCount` so that there is a thread for each core of
our processor.

```csharp
private static readonly int _threadCount = Environment.ProcessorCount;
```

We can then also increase the number of actions that we run in total, to say 55.
I also increase the Thread.Sleep interval to a full second, although this
is entirely optional.

```csharp
_actionCount = 55;
for (int index = 1; index <= 55; ++index)
```

This should work predictably, but we can expose an issue by utilizing thread
local storage. Instead of an `int mod = 10 * index` every iteration, we can utilize
a single `AsyncLocal<int> mod` and set it for each iteration. We then use `mod.Value`
each time we use the mod value. This forces us to use thread local storage.

```csharp
_actionCount = 55;
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    MyThreadPool.QueueUserWorkItem(() =>
    {
        IEnumerable<int> values = Produce(
            1 + mod.Value, 5 + mod.Value,
            1001 + mod.Value, 1005 + mod.Value);
        Consume(identifier, values);
    });
}

_resetEvent.Wait(cancellationToken);
```

However, when we do this and run the application, we find that every single iteration is
treated as if mod was just 10. They are not getting the 10 * index value that we expect!
To fix this, we must add support for execution contexts in our thread pool.

Importantly, this is also used in GUI programming significantly. By forcing actions to
run on the execution context from which they were called, we can force tasks added to
our thread pool to run on the Display thread when needed. We will see how we can take
advantage of this in asynchronous code later, but for now we just need to add the
support for execution contexts to our thread pool.

In our queue, instead of just taking an `Action` we also need to include the associated
`ExecutionContext`, which might be `null`. When an action is added to the queue,
we then need to capture the Execution Context, and when it is run in a thread,
run the action with the execution context if it is not null. I add a private Execute
method that takes the action and execution context and runs it accordingly,
as we can reuse this logic later.

```csharp
static class MyThreadPool
{
    private static readonly int _threadCount = Environment.ProcessorCount;

    private static readonly BlockingCollection<(Action, ExecutionContext?)> _actionQueue = [];

    private static void Execute((Action, ExecutionContext?) queued)
    {
        (Action action, ExecutionContext? executionContext) = queued;
        if (executionContext is null)
        {
            action();
        }
        else
        {
            ExecutionContext.Run(executionContext, act => ((Action)act!).Invoke(), action);
        }
    }

    static MyThreadPool()
    {
        for (int i = 0; i < _threadCount; ++i)
        {
            new Thread(() =>
            {
                while (true)
                {
                    // Run on the execution context instead of invoking directly here!
                    Execute(_actionQueue.Take());
                }
            })
            { IsBackground = true }.Start();
        }
    }

    public static void QueueUserWorkItem(Action action)
    {
        _actionQueue.Add((action, ExecutionContext.Capture()));
    }
}
```

Now when we execute this code, we see the appropriate values printed to the screen,
as the actions are being run on the context that allows them to view the
thread local storage appropriately.


### 9. Use the Standard Thread Pool

Now we know enough about how the Thread Pool works, we can just switch over to the
standard ThreadPool, which includes several optimizations and makes our code cleaner
due to not having the ThreadPool around on us.

Simply, we can just comment out or delete the MyThreadPool class. Then use
`ThreadPool.QueueUserWorkItem`. Note, this requires a state variable parameter in
the queued action, which we did not add in ours. Lets utilize this to optimize
our thread, however. We will create a readonly record struct `ThreadPoolState`
that takes a String identifier and a async local int Mod value.

```csharp
readonly record struct ThreadPoolState(string Identifier, AsyncLocal<int> Mod);
```

Then, instead of using the captured lambda state, we pass a new instance of this
record into the QueueUserWorkItem, using the generic version of the method
for type safety.

```csharp
ThreadPool.QueueUserWorkItem<ThreadPoolState>(state =>
{
    IEnumerable<int> values = Produce(
        1 + state.Mod.Value, 5 + state.Mod.Value,
        1001 + state.Mod.Value, 1005 + state.Mod.Value);
    Consume(state.Identifier, values);
}, new(identifier, mod), true);
```

This is a very simple step after our work with a custom thread pool, but now
we are ready to begin building a Task structure that tracks work done on the
thread pool, instead of using our counter and reset event.

### 10. Custom Task Completion Class

We can easily see one major disadvantage of our thread pool code in that we
have to use a counter and reset event logic in order to track when something
has been completed on the thread pool. Additionally, we do not have a good way
to handle exceptions were they to occur in our actions on this thread pool,
certainly no nice way to bubble them to the main thread we queued the actions from.

This is the logic for the basic `Task` object that C# provides. Lets create a basic
`TaskCompletion` that allows us to track when a piece of work is finished or
when it encounters an exception.

This class will need a boolean field indicating if the work is completed, a nullable
Exception field to store caught exceptions from the work, a reset event to provide
a Wait method, and a Lock to make sure everything is thread safe. We will need a
public property indicating if the task is complete yet, as a boolean. As for methods,
we need one to set the result of the task as finished, one to set the exception,
and one to wait. Since setting the result and setting an exception both complete the
task, these will go to a private Complete method, which sets the completed field
to true, sets the exception if included, and sets the reset event for any waiting.
Wait just waits on the reset event.

Special attention is paid to the `Wait` method to re-throw an exception that was
set. We use `ExceptionDispatcher` to maintain deep stack trace information.


```csharp
class MyTaskCompletion
{
    private readonly Lock _synchronize = new();
    private bool _completed = false;
    private Exception? _exception = null;
    private readonly ManualResetEventSlim _waitEvent = new(false);

    public bool IsCompleted
    {
        get
        {
            lock (_synchronize)
            {
                return _completed;
            }
        }
    }

    private void Complete(Exception? ex)
    {
        lock (_synchronize)
        {
            if (_completed)
            {
                throw new InvalidOperationException("Cannot complete an already completed task.");
            }

            _completed = true;
            _exception = ex;

            _waitEvent.Set();
        }
    }

    public void SetResult()
    {
        Complete(null);
    }

    public void SetException(Exception ex)
    {
        Complete(ex);
    }

    public void Wait()
    {
        _waitEvent.Wait();

        if (_exception is not null)
        {
            ExceptionDispatchInfo.Throw(_exception);
        }
    }
}
```

Next, we can delete the old `_actionCount` and `_resetEvent` globals,
and update Consume to take a `MyTaskCompletion` and report completion
via it. We do this by wrapping Consume code in a try...catch block
and using SetResult at the end inside the try block, or SetException
inside the catch block.


```csharp
void Consume(
    string identifier,
    IEnumerable<int> values,
    MyTaskCompletion taskCompletion) // New parameter to track the task's completion with
{
    //Wrap the whole worker method in a try block
    try
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        foreach (int value in values)
        {
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");

        // set the task as complete
        taskCompletion.SetResult();
    }
    catch (Exception ex)
    {
        // set the task as complete, but with an error state
        taskCompletion.SetException(ex);
    }
}
```

Now, in the top level run code, we want to create a `List<MyTaskCompletion>`
and for each iteration we run on the thread pool, create a new 
`MyTaskCompletion`, send it into Consume, and add it to the list.
At the end, we then Wait on each Task, just like we Joined the prior threads.
I add a `MyTaskCompletion` property to the `ThreadPoolState` record
to maintain optimizations as well.

```csharp
readonly record struct ThreadPoolState(string Identifier, AsyncLocal<int> Mod, MyTaskCompletion TaskCompletion);
```

```csharp
// Create a list of the tasks to monitor
List<MyTaskCompletion> tasks = [];
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    // Create a task to send to the instance method to track the completion of the work and add it to the list
    MyTaskCompletion taskCompletion = new();
    ThreadPool.QueueUserWorkItem<ThreadPoolState>(state =>
    {
        IEnumerable<int> values = Produce(
            1 + state.Mod.Value, 5 + state.Mod.Value,
            1001 + state.Mod.Value, 1005 + state.Mod.Value);
        Consume(state.Identifier, values, state.TaskCompletion);
    }, new(identifier, mod, taskCompletion), true);
    tasks.Add(taskCompletion);
}

// Wait for all the tasks instead of the reset event
foreach (MyTaskCompletion task in tasks)
{
    task.Wait();
}
```

This part has been quite a lift to get to. The TaskCompletion class is
remarkably simple, although it introduces us to the Tasking pattern that we
will repeat again and again. In fact, this is very closely related to the
`TaskCompletionSource` that comes in .NET Core. We will evaluate that later
when we understand the Task structure a little better by expanding this to a full
`Task` like structure.

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

### 12. ContinueWith and WhenAll

This Task structure works great, but if we want to perform some action when a Task
is complete, we are entirely left to call Wait and then perform an action after that.
This is not ideal, and we want to add a ContinueWith as the standard `Task` has. This
will perform an operation once the Task has completed. We should then refactor our
Wait method to utilize this better. Finally, with this, we have all we need to
implement the WhenAll and get rid of the loop at the end of our Run code.

To implement ContinueWith, we will need to add some additional state fields
to store an action that should be run at completion. This should run on the
appropriate execution context, so we will capture that context and run the
ContinueWith on the thread pool with that context.

First, we add a private readonly record struct to the class to store continuation
data. We will have a field of this type that stores the current continuation, if
set.

We also remove the reset event field for waiting, as we will reconstruct that with
ContinueWith.

```csharp
private readonly record struct RunContinuation(
    Action? Continuation,
    ExecutionContext? ExecutionContext);


private RunContinuation _continuation = new(null, null);

```

Then we can add a private Execute method to execution the continuation, if
there is any. This will run it on the thread pool with the caught context.

```csharp
private static void Execute(RunContinuation continuation)
{
    if (continuation.Continuation is null)
    {
        return;
    }

    ThreadPool.QueueUserWorkItem<RunContinuation>(continuation =>
    {
        if (continuation.ExecutionContext is null)
        {
            continuation.Continuation!();
        }
        else
        {
            ExecutionContext.Run(continuation.ExecutionContext, act => ((Action)act!).Invoke(), continuation.Continuation);
        }
    }, continuation, true);
}
```

Now, in the Complete method, remove the signal on the reset event and
call execute with the current continuation.

```csharp
private void Complete(Exception? ex)
{
    lock (_synchronize)
    {
        if (_completed)
        {
            throw new InvalidOperationException("Cannot complete an already completed task.");
        }

        _completed = true;
        _exception = ex;

        // Run the continuation on the thread pool, no more wait event to set *here*
        Execute(_continuation);
    }
}
```

Now, we can add our ContinueWith method. We will use a private
SetContinuationUnprotected method to actually set the continuation data,
as we can avoid deadlocks when we refactor Wait this way.
If the Task is already complete at this point, we just immediately
execute the action on the captured context. Otherwise, we set
the continuation and let it be run upon completion.

```csharp
private void SetContinuationUnprotected(Action action)
{
    RunContinuation continuation = new(action, ExecutionContext.Capture());
    if (_completed)
    {
        Execute(continuation);
    }
    else
    {
        _continuation = continuation;
    }
}

public void ContinueWith(Action action)
{
    lock (_synchronize)
    {
        SetContinuationUnprotected(action);
    }
}
```


Finally, we will refactor Wait. If the Task is already completed,
we will just return immediately. Otherwise we will create a
manual reset event and set the Task's continuation to the
reset event's Set method. We can then wait on the reset event.

```csharp
public void Wait()
{
    // Refactor the Wait method to use the continuation to set a reset event created here.

    ManualResetEventSlim? reset = null;

    lock (_synchronize)
    {
        if (!_completed)
        {
            reset = new();
            SetContinuationUnprotected(reset.Set);
        }
    }

    reset?.Wait();

    if (_exception is not null)
    {
        ExceptionDispatchInfo.Throw(_exception);
    }
}
```

Finally, we can use the ContinueWith method we just created
to create an effective WhenAll that is not just a loop but actually
is represented as a Task that can be waited on, itself.

We start our WhenAll by creating a Task that will complete when
all of the tasks passed in are completed. We just need to create that logic.
We will have a counter set to the number of tasks that are
passed in to our WhenAll. Then we call ContinueWith on each of the
tasks with a continuation that decrements the counter. If the counter
hits 0, we set the WhenAll task as complete.

```csharp
public static MyTask WhenAll(params IEnumerable<MyTask> tasks)
{
    MyTask task = new();

    List<MyTask> useTasks = [.. tasks];
    if (useTasks.Count < 1)
    {
        task.SetResult();
    }
    else
    {
        int remaining = useTasks.Count;

        void Continuation()
        {
            if (Interlocked.Decrement(ref remaining) < 1)
            {
                task.SetResult();
            }
        }

        foreach (MyTask useTask in useTasks)
        {
            useTask.ContinueWith(Continuation);
        }
    }

    return task;
}
```

Finally, we just replace the last loop in our Run code with
a single line call to this new WhenAll.

```csharp
MyTask.WhenAll(tasks).Wait();
```

This step did an awful lot of work for what seems very little
gain so far. However, having the ability to do a ContinueWith
truly enables us to begin venturing into asynchronous code,
and out of simple concurrency.

### 13. Delay and Task&lt;TResult&gt;

As we want to move to actual asynchronous code, we first add in
a Task.Delay implementation so we can get off of Thread.Sleep.
Once this is complete, we can start returning Tasks from our
Producer as well, though we need to add a new `Task<TResult>`
class to handle Tasks with Return values.

First, the Delay is remarkably simple. We can just use a `Timer`
and set a Task as completed when the Timer runs out.

```csharp
public static MyTask Delay(int timeout)
{
    MyTask task = new();
    new Timer(_ => task.SetResult()).Change(timeout, -1);
    return task;
}
```

We then replace both Thread.Sleep calls with calls to this,
and call Wait on the returned Task.

```csharp
IEnumerable<int> Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd)
{
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        MyTask.Delay(1000).Wait();
        yield return value;
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        MyTask.Delay(1000).Wait();
        yield return value;
    }
}
```

This would be a very simple step if that is all we did, so we
will also create a `MyTask<TResult>` now. First, however, we make
the SetResult method of the normal MyTask virtual, and make the
private Complete method protected. Next, we create a new class
that inherits the original `MyTask` and takes 1 generic parameter.

The standard `Task<TResult>` is far more capable than we are going to
try to accomplish, but basically, we just need a Result property
that will wait for completion before returning, a method
to set the result with a Result value, and a ContinueWith method that
also passes the result value. This will need a field to
store the result. As a final precaution, we will have the
parameterless SetResult throw an exception if called.

```csharp
public class MyTask<TResult>
    : MyTask
{
    private TResult _result = default!;

    public TResult Result
    {
        get
        {
            Wait();
            return _result;
        }
    }

    public override void SetResult()
    {
        throw new InvalidOperationException();
    }

    public void SetResult(TResult value)
    {
        if (!IsCompleted)
        {
            _result = value;
            Complete(null);
        }
    }
}
```

Next, we update our producer to return `IEnumerable<MyTask<int>>`.
In each loop iteration, we then make a new `MyTask<int>`
and set the result to the value in the ContinueWith of the prior delay.
We no longer use Wait in the Produce then, instead doing `yield return`
with each of the `MyTask<int>` that we create.

```csharp
IEnumerable<MyTask<int>> Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd)
{
    // Update all of this to return MyTask<int> that finishes with the value after the delay.
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        MyTask<int> returnTask = new();
        MyTask.Delay(1000).ContinueWith(() => returnTask.SetResult(value));
        yield return returnTask;
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        MyTask<int> returnTask = new();
        MyTask.Delay(1000).ContinueWith(() => returnTask.SetResult(value));
        yield return returnTask;
    }
}
```


Our Consume method now also needs to update to take these on
each iteration. It also needs to print the Result property.
Note what this does. Technically, the Wait is happening in the Consume
more explicitly than the Produce now. This is an interesting fact,
and it exposes that simply decoupling with iterator methods actually
leaves us far more coupled than it immediately appears.

```csharp
void Consume(
    string identifier,
    IEnumerable<MyTask<int>> values)
{
    // Remove all the funny tracking we had to add before! We're back to just a normal looking method!
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    foreach (MyTask<int> value in values)
    {
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value.Result}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

Finally, just one line in the Run code gets this all working.

```csharp
List<MyTask> tasks = [];
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    tasks.Add(MyTask.Run(() =>
    {
        // Update to MyTask<int> values
        IEnumerable<MyTask<int>> values = Produce(
            1 + mod.Value, 5 + mod.Value,
            1001 + mod.Value, 1005 + mod.Value);
        Consume(identifier, values);
    }));
}

MyTask.WhenAll(tasks).Wait();
```

Running this, sometimes the code appears to be able to go even more
parallel than some prior steps have gotten us. The code is starting
to feel oddly asynchronous as well, although we are mostly just still
running with concurrency. This is still a great step forward, and
we can now start considering actual asynchrony.

### 14. Creating an Asynchronous Chain

It is time for the first truly asynchronous take on our code. The way
we are going to do this will be somewhat ugly and reminiscent of the
state machine code we had before, breaking a loop into redundant calls.
However, this will give us truly asynchronous code, and it will give us
important insights into how async/await works.

First, we need to upgrade our ContinueWith method and add a couple of
additional ones that allow us to do long chains of continuations.
First, we update our existing ContinueWith to return a Task that
completes when the continuation has completed.


```csharp
public MyTask ContinueWith(Action action)
{
    // Update to return a Task that completes once the continuation has completed
    MyTask task = new();

    lock (_synchronize)
    {
        SetContinuationUnprotected(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                task.SetException(ex);
                return;
            }

            task.SetResult();
        });
    }

    return task;
}
```

The `MyTask<TResult>` class also needs a new ContinueWith,
which should take a `Func<TResult>` that allows the
continuation function to handle the result of the first task.
We could easily think of a set of others to add here, including
synchronous continuations and an async continuation that returns
a value, possibly of a different type than TResult. We will do just
the one we actually need today, but you can easily imagine an expansion.

```csharp
public MyTask ContinueWith(Func<TResult> action)
{
    return ContinueWith(() => action(_result));
}
```

Finally, we have to update the Consume method. Produce is already
about as async as it can get with our current tools. However,
we need to completely rewrite Consume to iterate over the IEnumerable
it gets via ContinueWith instead of a foreach loop. This is
pseudo-recursive, in that it does not do true recursion, and yet
the ContinueWiths make it look very much so like it. It also
has a lot in common with a state machine. In the end, we return
a MyTask that completes when the whole IEnumerable has been iterated.

```csharp
public static MyTask Consume(
    string identifier,
    IEnumerable<MyTask<int>> values)
{
    MyTask task = new();

    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    IEnumerator<MyTask<int>> state = values.GetEnumerator();

    void MoveNext()
    {
        try
        {
            if (state.MoveNext())
            {
                state.Current.ContinueWith(value =>
                {
                    Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
                    MoveNext();
                });
                return;
            }
        }
        catch (Exception ex)
        {
            task.SetException(ex);
            return;
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
        task.SetResult();
    }

    MoveNext();

    return task;
}
```

Finally, our Run code simplifies because we no longer need to do the `MyTask.Run`
since Consume will return a task directly.

```csharp
List<MyTask> tasks = [];
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    // Just call Consume straight and add its returned task to the list
    IEnumerable<MyTask<int>> values = Produce(
        1 + mod.Value, 5 + mod.Value,
        1001 + mod.Value, 1005 + mod.Value);
    tasks.Add(Consume(identifier, values));
}

MyTask.WhenAll(tasks).Wait();
```

Running this, for the first time, we see the full behavior of asynchronous code.
Every iteration is immediately started on the main thread, but then quickly
offloaded to the thread pool. With every advance, each task jumps to the next
available thread in the pool, no longer sticking to a single thread.
This looks nowhere as pretty as async/await might, but we do have truly
asynchronous code here now.

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

### 17. Standard async/await

We now remove our custom task classes entirely, and use just the
standard Task classes. It will become immediately apparent that
we do not have all the tools we want for this yet, as Produce
will need more refactoring.

To begin, we create a DelayOnNumber method. This just delays for
a second before returning a given value. This will be an async
method, and the Task representing the operation is what our
new Produce will return each iteration.

While doing this, we want to also begin to appropriately use
the ConfigureAwait method on the standard Task object. By default,
every Task is set as if you called `ConfigureAwait(true)`. This
causes the current task to attempt to return to the same execution
context that it started on before awaiting on a called Task.
However, this can be a performance hit, and in the vast majority
of non-UI library code, you want to explicitly call `ConfigureAwait(false)`
when you await a Task, such that you can continue the rest of the
method on any thread, or any context. You may utilize the default
behavior to stay on the UI thread without using Dispatcher, which
makes this extremely useful in some cases, however.

```csharp
async Task<int> DelayOnNumber(
    int number)
{
    await Task.Delay(1000).ConfigureAwait(false);
    return number;
}
```

The updated Produce method now looks nicer, although the
DelayOnNumber thing is not our favorite.

```csharp
IEnumerable<Task<int>> Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd)
{
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        yield return DelayOnNumber(value);
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        yield return DelayOnNumber(value);
    }
}
```

Consume method simply changes to using all standard Tasks,
and we also add in `ConfigureAwait(false)` here.

```csharp
async Task Consume(
    string identifier,
    IEnumerable<Task<int>> values)
{
    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    foreach (Task<int> valueTask in values)
    {
        int value = await valueTask.ConfigureAwait(false);
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

We also can use `ConfigureAwait(false)` in our Run code if we want.

```csharp
await Task.WhenAll(tasks).ConfigureAwait(false);
```

We now have standard async/await code, and we should understand
it pretty clearly through this work. We have several pieces to
improve on yet, but this is very strong code.

### 18. Task Completion Source

The first thing we want to do is show how we can use the standard
classes to achieve some of the same patterns we were doing with
our custom Task at times. We frequently created a Task and then
did SetResult or SetException in another process to signal
when it was completed. The standard Task classes do not allow us
to do this directly, but a separate `TaskCompletionSource` was
created that enables this pattern for us.

A good example of where this might be useful is some long running
process that is better served on a managed, dedicated thread. Any
operation potentially leading to thread exhaustion might be better
served in this pattern in async code.

We will re-create our DoubleLoop from our first procedural sample
that got us started. This time, it will take a TaskCompletionSource
and signal to the source when it is completed or an exception has occurred.

```csharp
public static void DoubleLoop(
    string identifier,
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    TaskCompletionSource completionSource)
{
    // Almost identical to step 1's DoubleLoop, but completionSource is a TaskCompletionSource.
    try
    {
        Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

        for (int value = firstStart; value <= firstEnd; ++value)
        {
            Thread.Sleep(1000);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }
        for (int value = secondStart; value <= secondEnd; ++value)
        {
            Thread.Sleep(1000);
            Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
        }

        Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");

        completionSource.SetResult();
    }
    catch (Exception ex)
    {
        completionSource.SetException(ex);
    }
}
```

Now, we just launch this as a separate thread and we can await
on the Task provided by the TaskCompletionSource.

```csharp
List<Task> tasks = [];
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    IEnumerable<Task<int>> values = Produce(
        1 + mod.Value, 5 + mod.Value,
        1001 + mod.Value, 1005 + mod.Value);
    tasks.Add(Consume(identifier, values));
}

// We delay a short time and then spin off a background thread, with a ThreadCompletionSource to track its progress.
// the Thread from the ThreadCompletionSource is added to the tasks lists to wait on.
await Task.Delay(500).ConfigureAwait(false);
TaskCompletionSource backThreadSource = new();
Thread instanceCaller = new(new ThreadStart(() =>
    DoubleLoop("Single Thread",
        1, 5,
        101, 105,
        backThreadSource)));
instanceCaller.Start();
tasks.Add(backThreadSource.Task);

await Task.WhenAll(tasks).ConfigureAwait(false);
```


This is a pretty simple tangent, and is not anything particularly
new. However, this is an important pattern to know, and it can
be utilized in many places in asynchronous code.


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

### 20. Introducing IAsyncDisposable for CancellationTokenRegistration

The standard CancellationToken has many features we have not implemented
in our quick example. One of these includes a structure that allows us
to easily unregister callbacks that have been registered. This structure
implements the standard IDisposable interface, and it also provides
a good time to introduce the asynchronous IAsyncDisposable.

First, we add an Unregister method to our MyCancellationTokenSource.

```csharp
public void Unregister(Action callback)
{
    lock (_callbacks)
    {
        if (!_isCancellationRequested && _callbacks.Contains(callback))
        {
            _callbacks.Remove(callback);
        }
    }
}
```

Next, we will create a small struct called `MyCancellationTokenRegistration`.
This will take a `MyCancellationTokenSource` and a callback `Action` in
the primary constructor. It will implement both `IDisposable` and
`IAsyncDisposable`. We will not include the full dispose pattern here,
as that is well beyond needs of this tutorial.

This gives us 2 methods to implement: Dispose and DisposeAsync. DisposeAsync
returns a ValueTask, which can also be used in async methods much like
Task. However, this provides some optimizations in particular scenarios.
Most will not use ValueTask regularly, but IAsyncDisposable is a common case.

```csharp
public readonly struct MyCancellationTokenRegistration(
    MyCancellationTokenSource source, Action callback)
    : IDisposable, IAsyncDisposable
{
    public void Dispose()
    {
        source.Unregister(callback);
    }

    public async ValueTask DisposeAsync()
    {
        source.Unregister(callback);
    }
}
```

Now, we update our Register method to return one of these structures,
allowing callbacks to be unregistered via this interface.

```csharp
public MyCancellationTokenRegistration Register(Action callback)
{
    lock (_callbacks)
    {
        if (!_isCancellationRequested)
        {
            _callbacks.Add(callback);
            return new(this, callback);
        }
    }

    callback();
    return new(this, callback);
}
```


Now we can handle this in our Run method where we add a 
registration. For example, we can now use `await using` and have
DisposeAsync called appropriately.

```csharp
await using MyCancellationTokenRegistration cancelRegister = cts.Register(() =>
{
    Console.WriteLine("Registered cancellation.");
});
```

In addition, we can call Dispose or DisposeAsync anywhere we wish
to unregister at as well.

```csharp
await cancelRegister.DisposeAsync().ConfigureAwait(false);
```

This step is mostly expanded to introduce the concept of the
`IAsyncDisposable` interface. Knowing it, we can now move on to
the standard cancellation token comfortably.


### 21. Standard Cancellation Tokens

The basic next step is to remove the custom cancellation token source
and token struct. Replace all instances of MyCancellationToken
with just the standard CancellationToken.

When we do this, we can now also pass the canceled token in to
a TaskCompletionSource, providing even mroe information when things
happen in our code.

```csharp
void DoubleLoop(
    string identifier,
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    TaskCompletionSource completionSource,
    CancellationToken cancellationToken)
{
    // Replace the parameter with the standard cancellation token type
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

        cancellationToken.ThrowIfCancellationRequested();
        Console.WriteLine($"Fin  {identifier} / {Environment.CurrentManagedThreadId}");

        completionSource.SetResult();
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        // With the standard cancellation token, we can send the canceled token into our task completion source for more information to the caller
        completionSource.SetCanceled(cancellationToken);
    }
    catch (Exception ex)
    {
        completionSource.SetException(ex);
    }
}
```

We can now also pass the standard token down to other calls,
such as Task.Delay.

```csharp
async Task<int> DelayOnNumber(
    int number,
    CancellationToken cancellationToken)
{
    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
    return number;
}
```

And our Run code now just uses the standard CancellationTokenSource.
Note that in the samples, each one is passed in a standard Token,
and so we can use this to create a Linked Token Source as well.
This is shown here for demonstration, but you can skip it if you
are following along.

```csharp
CancellationTokenSource cts = new();//new(4500);

CancellationTokenSource linked =
    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

await using CancellationTokenRegistration cancelRegister = linked.Token.Register(() =>
{
    Console.WriteLine("Registered cancellation.");
});

List<Task> tasks = [];
AsyncLocal<int> mod = new();
for (int index = 1; index <= 55; ++index)
{
    mod.Value = 10 * index;
    string identifier = $"Action {index}";
    IEnumerable<Task<int>> values = Produce(
        1 + mod.Value, 5 + mod.Value,
        1001 + mod.Value, 1005 + mod.Value,
        cts.Token);
    tasks.Add(Consume(identifier, values, cts.Token));
}

//We can pass the cancellation token down now that we know what to do!
await Task.Delay(500, cancellationToken).ConfigureAwait(false);
TaskCompletionSource backThreadSource = new();
Thread instanceCaller = new(new ThreadStart(() =>
    DoubleLoop("Single Thread",
        1, 5,
        101, 105,
        backThreadSource, linked.Token)));
instanceCaller.Start();
tasks.Add(backThreadSource.Task);

try
{
    await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
    // the standard cancel has an async cancel method
    // note: this one can cause problems with the UI thread in WPF, etc., so can't
    //   always be used, but it is nice when we can!
    await cts.CancelAsync().ConfigureAwait(false);

    await Task.WhenAll(tasks).ConfigureAwait(false);

    Console.WriteLine("All fin");
}
catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
{
    Console.WriteLine("Canceled");
}
```

Finally, we are now appropriately armed for moving forward to clean
the most clumsy part of our code still remaining. We are well armed
with the standard tools of asynchronous code that handle single objects,
and we have our code nicely separated into something like Producer
and Consumer.

### 22. Creating IAsyncEnumerable/IAsyncEnumerator Implementations

Instead of the kind of weird mix of asynchronous and synchronous code
in our Produce method, we want to use IAsyncEnumerable, which allows
us to use await and yield return together more fluidly. Our first step
at this will be to construct our own IAsyncEnumerable implementation.
This will be almost identical to step 4, and in fact, let’s just update
Step 4’s IEnumerable and IEnumerator instances to IAsyncEnumerable
and IAsyncEnumerator.

The state machine inside our enumerator will be almost identical but we 
can switch the Thread.Sleep for Task.Delay calls to make it more async. 
Otherwise, we just need to follow the interfaces to make the properties and methods async.

```csharp
public class Producer(
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    CancellationToken cancellationToken)
    : IAsyncEnumerator<int>
{
    public void Reset()
    {
        _position = Position.Initial;
        Current = -1;
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
    }

    private enum Position
    {
        Initial,
        FirstLoop,
        SecondLoop,
        End
    }

    private Position _position = Position.Initial;

    public int Current { get; private set; } = -1;

    public async ValueTask<bool> MoveNextAsync()
    {
        async ValueTask<bool> FirstLoop()
        {
            if (Current <= firstEnd)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                return true;
            }

            _position = Position.SecondLoop;
            Current = secondStart;
            return await SecondLoop().ConfigureAwait(false);
        }

        async ValueTask<bool> SecondLoop()
        {
            if (Current <= secondEnd)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                return true;
            }

            _position = Position.End;
            return false;
        }

        switch (_position)
        {
            case Position.Initial:
                Current = firstStart;
                _position = Position.FirstLoop;
                return await FirstLoop().ConfigureAwait(false);

            case Position.FirstLoop:
                ++Current;
                return await FirstLoop().ConfigureAwait(false);

            case Position.SecondLoop:
                ++Current;
                return await SecondLoop().ConfigureAwait(false);

            default:
                throw new InvalidOperationException();
        }
    }
}

public class ProductionEnumerable(int firstStart, int firstEnd, int secondStart, int secondEnd)
    : IAsyncEnumerable<int>
{
    public IAsyncEnumerator<int> GetAsyncEnumerator(
        CancellationToken cancellationToken)
    {
        return new Producer(firstStart, firstEnd, secondStart, secondEnd, cancellationToken);
    }
}
```

Now we update Consume to take an IAsyncEnumerable and use await foreach, which results in nicer looking code than our last step.

```csharp
async Task Consume(
    string identifier,
    IAsyncEnumerable<int> values,
    CancellationToken cancellationToken)
{
    // Update to taking an IAsyncEnumerable

    Console.WriteLine($"Writing values: {identifier} / {Environment.CurrentManagedThreadId}");

    await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
    {
        Console.WriteLine($"{identifier} / {Environment.CurrentManagedThreadId} => {value}");
    }

    Console.WriteLine($"Fin {identifier} / {Environment.CurrentManagedThreadId}");
}
```

And our top level code also gets updated to the new IAsyncEnumerable:

```csharp
IAsyncEnumerable<int> values = new ProductionEnumerable(
    1 + mod.Value, 5 + mod.Value,
    1001 + mod.Value, 1005 + mod.Value);
tasks.Add(Consume(identifier, values, cancellationToken));
```

In the samples, I also clean up some of the excessive cancellation token work in the Run code at this point.
This is optional.

Of course, just as we did not just stay with this big implementation of IEnuemrable, we are
not going to stay with this big implementation of IAsyncEnumerable. Rather, we can let the
compiler generate most of this work for us.

### 23. IAsyncEnumerable Iterator Methods

Just as we replaced the IEnumerable implementation with an IEnumerable and using yield return,
we do the exact same thing here. We instead return IAsyncEnumerable, and we mark the
method as async, using await directly in it. That is, we can use yield return and await
together in this. Knowing how it is compiled helps us understand how it works.

We should certainly also take in a CancellationToken in this method. When doing so,
we now add the EnumeratorCancellation attribute on said parameter, allowing the
compiler to take additional optimizations for us.


```csharp
async IAsyncEnumerable<int> Produce(
    int firstStart, int firstEnd, int secondStart, int secondEnd,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    for (int value = firstStart; value <= firstEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        yield return value;
    }
    for (int value = secondStart; value <= secondEnd; ++value)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        yield return value;
    }
}
```

Our Consume already handles this fine from the previous step, so we just update the top level to call this new Produce and we see the familiar output.

```csharp
IAsyncEnumerable<int> values = Produce(
    1 + mod.Value, 5 + mod.Value,
    1001 + mod.Value, 1005 + mod.Value,
    cancellationToken);
tasks.Add(Consume(identifier, values, cancellationToken));
```
We now just have a nice 2 methods for our Produce/Consume setup once again, and the output remains the same as it has the entire time so far.
You can delete the custom IAsyncEnumerator implementation at this point as we will no longer even refer to it, preferring to simply use the
iterator methods like this instead moving forward.

The only problem with what we have now is that we are still tied to a single producer and a single consumer.
We cannot support multiple producers on one channel or multiple consumers on one channel. This is explicitly still only one-to-one.


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

### 27. Extending Channels Pipelines with a Middleman

One of the benefits of this highly decoupled pattern is that it is much easier to extend the data pipeline with middlemen.
Here, we will create one. This middleman will consume values from the Producer and perform some task on them prior to sending
them to the Consumer. For this, we will have our own private Channel to send modified messages on, and so it will look similar
to the Producer in structure but closer to the Consumer in code. Let’s look at the basic structure first.

```csharp
class Middleman
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();
    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
 
    public async Task Intercept(
        IAsyncEnumerable<int> values,
        CancellationToken cancellationToken)
    {
 
    }
}
```

We can take the Consume from our Consumer as a baseline, but instead of printing to the screen,
we will perform some action and write the result to our own channel.
For this sample, we will collect 2 messages together and send them as 1.
The first value will be multiplied by 100000 and added to the second value; this sum will be the value sent to Consumer.
For this, we will have a private nullable int field for the last value and a semaphore to make sure we don’t have race conditions.


```csharp
private int? _lastValue = null;
 
private readonly SemaphoreSlim _synchronize = new(1);
 
private async Task Consume(
    IAsyncEnumerable<int> values,
    CancellationToken cancellationToken)
{
    await foreach (int value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
    {
        await _synchronize.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_lastValue is null)
            {
                _lastValue = value;
            }
            else
            {
                await _channel.Writer.WriteAsync(
                    (100000 * _lastValue.Value) + value,
                    cancellationToken).ConfigureAwait(false);
                _lastValue = null;
            }
        }
        finally
        {
            _synchronize.Release();
        }
    }
}
```

Then let’s fill in our Intercept method defined above based basically the same as the Producer, but calling Consume here.
Let’s call it some ridiculous number of times like 666.

```csharp
public async Task Intercept(
    IAsyncEnumerable<int> values,
    CancellationToken cancellationToken)
{
    List<Task> consumers = [];
    for (int index = 1; index <= 666; ++index)
    {
        consumers.Add(Consume(
            values,
            cancellationToken));
    }
    await Task.WhenAll(consumers).ConfigureAwait(false);
    _channel.Writer.Complete();

}
```

Finally, we update the top-level statements to place our Middleman in between the producer and consumer, intercepting all messages from producer.

```csharp
Producer producer = new(55);
Consumer consumer = new();
Middleman middleman = new();
_ = consumer.Run(middleman.ReadAllAsync(cancellationToken), cancellationToken);
_ = middleman.Intercept(producer.ReadAllAsync(cancellationToken), cancellationToken);
List<Task> tasks = [producer.Run(cancellationToken)];
```

Now, when this is run, you’ll see about half of the messages, instead clearly showing the middleman merging 2 values at a time as described.
All kinds of interesting pipeline logic can be constructed, this only serving as a basic example.