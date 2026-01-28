### 1. Simple Procedural Code

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

#### Navigation

[Full Sample](Samples/01ProceduralSample.cs)

[Home](/)

[Next: First Producer: Produce a List](02ListProducerSample.md)

#### Full Navigation

##### 1. Conceptual Setup

1. [Simple Procedural Code](01ProceduralSample.md)
1. [First Producer: Produce a List](02ListProducerSample.md)
1. [Making Producer Sleep: State Machine](03StateMachineProducerSample.md)
1. [Use IEnumerable/IEnumerator Interfaces](04IEnumerableProducerSample.md)
1. [Use Iterator Methods](05IteratorProducerSample.md)

##### 2. Multithreading

6. [Make it Multithreaded](06ThreadSample.md)
1. [Make a Custom Thread Pool](07MyThreadPoolSample.md)
1. [Handle Thread Local Storage and Execution Contexts](08MyThreadPoolWithContextSample.md)
1. [Use the Standard Thread Pool](09ThreadPoolSample.md)

##### 3. Tasking Structure

10. [Custom Task Completion Class](10MyTaskCompletionSample.md)
1. [Custom Task Class](11MyTaskSample.md) 
1. [Implementing ContinueWith and WhenAll](12MyTaskWhenAllSample.md)
1. [Implementing Delay and Task&lt;TResult&gt;](13MyTaskDelaySample.md)

##### 4. Async/Await

14. [Creating an Asynchronous Chain with ContinueWith](14MyTaskAsyncChainSample.md)
1. [Simulate async/await with Iterators](15IterateTaskGeneratorSample.md)
1. [Using actual async/await with MyTask](16AwaitableCustomSample.md)
1. [Standard async/await](17StdAwaitSample.md)

##### 5. Asynchronous Utilities

18. [Task Completion Source](18TaskCompletionSourceSample.md)
1. [Constructing Cancellation Tokens](19MyCancellationTokenSample.md) 
1. [Introducing IAsyncDisposable for CancellationTokenRegistration](20IAsyncDisposableSample.md)
1. [Standard Cancellation Tokens](21CancellationTokenSample.md)
1. [Creating IAsyncEnumerable/IAsyncEnumerator Implementations](22CustomAsyncEnumerableSample.md)
1. [IAsyncEnumerable Iterator Methods](23IAsyncEnumerableIteratorSample.md)

##### 6. Asynchronous Channels

24. [Custom Channels Implementation](24MyChannelSample.md)
1. [Standard Channels](25ChannelsSample.md)
1. [Structuring a Channels Pipeline](26StructuredChannelsSample.md)
1. [Extending Channels Pipelines with a Middleman](27ChannelMiddlemanSample.md)

##### 7. Dataflow

28. [Introduce Dataflow in the Middleman](28DataFlowMiddlemanSample.md)
1. [Replace Channels Pipeline with Dataflow Blocks](29DataFlowCompleteSample.md)
