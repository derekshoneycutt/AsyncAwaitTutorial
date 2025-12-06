# Async/Await Tutorial Samples


The point of this project is to provide a series of 30 samples that progressively build to
and upon the basic idea of async/await in C#. Each example essentially builds on the
example prior, and so comparing to previous samples is a good way to understand what step
is being taken, and possibly why.


Some basic familiarity with C# and Threads is assumed, although this could
probably be useful for someone with familiarity with other languages understanding how
async/await code works in C# as well, as well as the idea of Threads is grasped already.



The progression of samples can be taken as follows:

#### Threading
We start with basic threading and thread pools

1. Procedural Baseline : Creates a baseline of 2 for loops in a procedural, synchronous sample.
1. Threads : Opens several threads to demonstrate basic concurrency
1. Custom Thread Pool : Introduces the basic idea of a thread pool with a couple of reusable threads
1. Custom Thread Pool with ExecutionContext : Utilizes ExecutionContext to maintain appropriate thread local storage across a thread pool
1. ThreadPool : Uses the built in C# ThreadPool instead of a custom one

#### Tasking
Next, we introduce the basic concept of Tasks and show how asynchronous code gets started

6. Custom Task Completion : Demonstrates creating a basic task completion class to track tasks on the thread pool
1. Custom Task : Demonstrates the logic behind full Task with a custom task class
1. Custom WhenAll : Demonstrates how WhenAll is created via custom task
1. Custom Delay : Creates and uses an asynchronous Delay instead of Thread.Sleep. First significant change in behavior!
1. Asynchronous chain with custom Task : First real, fully asynchronous solution
1. Asynchronous chain with Standard Task : Applies everything so far to the standard Task class

#### State Machines and IEnumerable Tangent
In order to arrive at async/await, we take a detour through state machines and iterators in C#,
as iterators share significant code with async/await after compile.

12. State Machine : Demonstrates a basic C-style state machine for reference
1. IEnumerable from the ground up : Demonstrates IEnumerable iterator as a specified form of previous state machine
1. IEnumerable iterator / yield return : Uses compiler sugar to make iterators easy to write and read

#### Async/Await
Now, we finally build and work with the real async/await in C#.

15. Task iterator : Demonstrates iterating over Tasks returned by yield return to simulate async/await (Uses custom task class because TaskCompletionSource not introduced yet)
1. Custom await-able : Demonstrates how a task can become available to await -- first real use of async/await
1. Async/Await : First full, basic demonstration of async/await with standard Tasks only. Includes ConfigureAwait(false) applied everywhere.

#### Essential Async Utilities
An important part of programming with async/await is knowing how to synchronize resources and operations, including cancellation.
Semaphores were used in a previous example and are good, but otherwise, we have TaskCompletionSource and CancellationToken.



18. TaskCompletionSource : Demonstrates how to represent synchronous or managed operations alongside asynchronous code via a custom managed Thread and TaskCompletionSource
1. Semaphores : Demonstrates coordinating async methods with SemaphoreSlim. We avoid mutex, monitor, critical sections, and other blocking constructs in async/await code in C#.
1. Custom CancellationToken : Demonstrates the structure of cancellation tokens with custom types to handle cancellation as a first pass.
1. CancellationToken : Demonstrate the use of standard cancellation tokens with asynchronous code. This fully brings everything so far into one sample.
1. IAsyncDisposable : Demonstrates the use of IAsyncDisposable and await using for asynchronous disposal.

#### IAsyncEnumerable

With async/await introduced, we also have the asynchronous collection of IAsyncEnumerable.
This can be used for many purposes, including async events in C#. Here we demonstrate the basics.

23. Enumerable of Tasks : Gets a basic feel for the logic behind IAsyncEnumerable
1. Custom IAsyncEnumerable implementation : Demonstrates implementing IAsyncEnumerable and IAsyncEnumerator and then using await foreach
1. IAsyncEnumerator iterator with await and yield return : Demonstrates using async/await and yield return together in an IAsyncEnumerator iterator method

#### Channels and Observables

Channels start on the more advanced path of async/await, providing special producer/consumer patterns themselves.
We start by constructing a custom channel class to demonstrate the purpose and structure,
and then construct a sample with the standard channels with far mroe features. We then evaluate
how this might compare to the IObservable/IObserver pattern to bring everything together.

26. Custom Channels : Demonstrates a custom, basic channel class that shows the basic motivations and how it works
1. Channels : Demonstrates utilizing the standard Channels in Producer/Consumer asynchronous pattern.
1. Structured Channels : Demonstrates utilizing Channels in a structured way to demonstrate a stream of values from a central producer class.
1. IObservable : Demonstrates how you can turn an IAsyncEnumerable into an IObservable and utilize that to handle all events.
1. IAsyncObservable : Extends the IObservable sample with new IAsyncObservable and IAsyncObserver patterns for further demonstration of async.

