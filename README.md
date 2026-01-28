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

### 1. Conceptual Setup

1. [Simple Procedural Code](01ProceduralSample.md)
1. [First Producer: Produce a List](02ListProducerSample.md)
1. [Making Producer Sleep: State Machine](03StateMachineProducerSample.md)
1. [Use IEnumerable/IEnumerator Interfaces](04IEnumerableProducerSample.md)
1. [Use Iterator Methods](05IteratorProducerSample.md)

### 2. Multithreading

6. [Make it Multithreaded](06ThreadSample.md)
1. [Make a Custom Thread Pool](07MyThreadPoolSample.md)
1. [Handle Thread Local Storage and Execution Contexts](08MyThreadPoolWithContextSample.md)
1. [Use the Standard Thread Pool](09ThreadPoolSample.md)

### 3. Tasking Structure

10. [Custom Task Completion Class](10MyTaskCompletionSample.md)
1. [Custom Task Class](11MyTaskSample.md) 
1. [Implementing ContinueWith and WhenAll](12MyTaskWhenAllSample.md)
1. [Implementing Delay and Task&lt;TResult&gt;](13MyTaskDelaySample.md)

### 4. Async/Await

14. [Creating an Asynchronous Chain with ContinueWith](14MyTaskAsyncChainSample.md)
1. [Simulate async/await with Iterators](15IterateTaskGeneratorSample.md)
1. [Using actual async/await with MyTask](16AwaitableCustomSample.md)
1. [Standard async/await](17StdAwaitSample.md)

### 5. Asynchronous Utilities

18. [Task Completion Source](18TaskCompletionSourceSample.md)
1. [Constructing Cancellation Tokens](19MyCancellationTokenSample.md) 
1. [Introducing IAsyncDisposable for CancellationTokenRegistration](20IAsyncDisposableSample.md)
1. [Standard Cancellation Tokens](21CancellationTokenSample.md)
1. [Creating IAsyncEnumerable/IAsyncEnumerator Implementations](22CustomAsyncEnumerableSample.md)
1. [IAsyncEnumerable Iterator Methods](23IAsyncEnumerableIteratorSample.md)

### 6. Asynchronous Channels

24. [Custom Channels Implementation](24MyChannelSample.md)
1. [Standard Channels](25ChannelsSample.md)
1. [Structuring a Channels Pipeline](26StructuredChannelsSample.md)
1. [Extending Channels Pipelines with a Middleman](27ChannelMiddlemanSample.md)

### 7. Dataflow

28. [Introduce Dataflow in the Middleman](28DataFlowMiddlemanSample.md)
1. [Replace Channels Pipeline with Dataflow Blocks](29DataFlowCompleteSample.md)
