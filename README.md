# Async/Await Tutorial Samples


The point of this project is to provide a series of 21 samples that progressively build to
and upon the basic idea of async/await in C#. Each example essentially builds on the
example prior, and so comparing to previous samples is a good way to understand what step
is being taken, and possibly why.

I would not consider an engineer complete with basic async/await until they have reviewed
and understood Sample 17, with CancellationToken use. IAsyncEnumerable is also extremely
valuable in modern C#, and Channels help them, so I would personally recommend counting
all 21 absolutely essential. That said, many engineers stop at a loose understanding up to
about sample 15, missing details such as the ExecutionContext and ConfigureAwait along the way.
They should consider ExecutionContext and CancellationToken, at the very least.


Some basic familiarity with C# and Threads is assumed, although this could
probably be useful for someone with familiarity with other languages understanding how
async/await code works in C# as well, as well as the idea of Threads is grasped already.


The progressoin of samples is as follows:

1. Threads : Opens several threads to demonstrate basic concurrency
1. Custom Thread Pool : Introduces the basic idea of a thread pool with a couple of reusable threads
1. Custom Thread Pool with ExecutionContext : Utilizes ExecutionContext to maintain appropriate thread local storage across a thread pool
1. ThreadPool : Uses the built in C# ThreadPool instead of a custom one
1. Custom Task : Demonstrates the logic behind Task with a custom task class
1. Custom WhenAll : Demonstrates how WhenAll is created via custom task
1. Custom Delay : Creates and uses an asynchronous Delay instead of Thread.Sleep. First significant change in behavior!
1. Asynchronous chain with custom Task : First real, fully asynchronous solution
1. Asynchronous chain with Standard Task : Applies everything so far to the standard Task class
1. State Machine : Demonstrates a basic C-style state machine for reference
1. IEnumerable from the ground up : Demonstrates IEnumerable iterator as a specified form of previous state machine
1. IEnumerable iterator / yield return : Uses compiler sugar to make iterators easy to write and read
1. Task iterator : Demonstrates iterating over Tasks returned by yield return to simulate async/await (Uses custom task class because TaskCompletionSource not introduced yet)
1. Custom await-able : Demonstrates how a task can become available to await -- first real use of async/await
1. Async/Await : First full, basic demonstration of async/await with standard Tasks only. Includes ConfigureAwait(false) applied everywhere.
1. TaskCompletionSource : Demonstrates how to represent synchronous or managed operations alongside asynchronous code via a custom managed Thread and TaskCompletionSource
1. CancellationToken : Demonstrate the use of cancellation tokens with asynchronous code. This fully brings everything so far into one sample.
1. Enumerable of Tasks : Gets a basic feel for the logic behind IAsyncEnumerable
1. Custom IAsyncEnumerable implementation : Demonstrates implementing IAsyncEnumerable and IAsyncEnumerator and then using await foreach
1. IAsyncEnumerator iterator with await and yield return : Demonstrates using async/await and yield return together in an IAsyncEnumerator iterator method
1. Channels : Brings everything together in a simple demonstration of the Producer/Consumer asynchronous pattern in C# with Channels


