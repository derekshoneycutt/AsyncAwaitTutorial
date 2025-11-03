# Async/Await Tutorial Samples


The point of this project is to provide a series of 21 samples that progressively build to
and upon the basic idea of async/await in C#. Each example essentially builds on the
example prior, and so comparing to previous samples is a good way to understand what step
is being taken, and possibly why.

The progression of samples can be taken as follows:

#### Threading
We start with basic theading and thread pools

1. Threads : Opens several threads to demonstrate basic concurrency
1. Custom Thread Pool : Introduces the basic idea of a thread pool with a couple of reusable threads
1. Custom Thread Pool with ExecutionContext : Utilizes ExecutionContext to maintain appropriate thread local storage across a thread pool
1. ThreadPool : Uses the built in C# ThreadPool instead of a custom one

#### Tasking
Next, we introduce the basic concept of Tasks and show how asynchronous code gets started

5. Custom Task : Demonstrates the logic behind Task with a custom task class
1. Custom WhenAll : Demonstrates how WhenAll is created via custom task
1. Custom Delay : Creates and uses an asynchronous Delay instead of Thread.Sleep. First significant change in behavior!
1. Asynchronous chain with custom Task : First real, fully asynchronous solution
1. Asynchronous chain with Standard Task : Applies everything so far to the standard Task class

#### State Machines and IEnumerable Tangent
In order to arrive at async/await, we take a detour through state machines and iterators in C#,
as iterators share significant code with async/await after compile.

10. State Machine : Demonstrates a basic C-style state machine for reference
1. IEnumerable from the ground up : Demonstrates IEnumerable iterator as a specified form of previous state machine
1. IEnumerable iterator / yield return : Uses compiler sugar to make iterators easy to write and read

#### Async/Await
Now, we finally build and work with the real async/await in C#.

13. Task iterator : Demonstrates iterating over Tasks returned by yield return to simulate async/await (Uses custom task class because TaskCompletionSource not introduced yet)
1. Custom await-able : Demonstrates how a task can become available to await -- first real use of async/await
1. Async/Await : First full, basic demonstration of async/await with standard Tasks only. Includes ConfigureAwait(false) applied everywhere.

#### Essential Async Utilities
An important part of programming wiht async/await is knowing how to synchronize resources and operations, including cancellation.
Semaphores were used in a previous example and are good, but otherwise, we have TaskCompletionSource and CancellationToken.

We avoid mutuxes, critical sections, and other blocking constructs in async/await code in C#.

16. TaskCompletionSource : Demonstrates how to represent synchronous or managed operations alongside asynchronous code via a custom managed Thread and TaskCompletionSource
1. CancellationToken : Demonstrate the use of cancellation tokens with asynchronous code. This fully brings everything so far into one sample.

#### IAsyncEnumerable

With async/await introduced, we also have the asynchronous collection of IAsyncEnumerable.
This can be used for many purposes, including async events in C#. Here we demonstrate the basics.

18. Enumerable of Tasks : Gets a basic feel for the logic behind IAsyncEnumerable
1. Custom IAsyncEnumerable implementation : Demonstrates implementing IAsyncEnumerable and IAsyncEnumerator and then using await foreach
1. IAsyncEnumerator iterator with await and yield return : Demonstrates using async/await and yield return together in an IAsyncEnumerator iterator method

#### Channels

Channels start on the more advanced path of async/await, providing special producer/consumer patterns themselves.
We construct a sample hee that should now be pretty easily understood based on the prior async samples.

21. Channels : Brings everything together in a simple demonstration of the Producer/Consumer asynchronous pattern in C# with Channels


I would not consider an engineer complete with basic async/await until they have reviewed
and understood Sample 17, with CancellationToken use. IAsyncEnumerable is also extremely
valuable in modern C#, and Channels help them, so I would personally recommend counting
all 21 absolutely essential. That said, many engineers stop at a loose understanding up to
about sample 15, missing details such as the ExecutionContext and ConfigureAwait along the way.
They should consider ExecutionContext and CancellationToken, at the very least.


Some basic familiarity with C# and Threads is assumed, although this could
probably be useful for someone with familiarity with other languages understanding how
async/await code works in C# as well, as well as the idea of Threads is grasped already.


