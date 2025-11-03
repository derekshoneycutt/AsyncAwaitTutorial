# Async/Await Tutorial Samples


The point of this project is to provide a series of 21 samples that progressively build the idea
of async/await in C#. The progressoin of samples is as follows:

1. Threads : Opens several threads to demonstrate basic concurrency
1. Custom Thread Pool : Introduce the basic idea of a thread pool with a couple of reusable threads
1. Custom Thread Pool w/ ExecutionContext : Utilizing ExecutionContext to maintain appropriate thread local storage across a thread pool
1. ThreadPool : Using the built in C# ThreadPool instead of a custom one
1. Custom Task : Demonstrate the logic behind Task with a custom task class
1. WhenAll with Custom Task : Demonstrate how WhenAll is created via custom task
1. Delay : Create and use an asynchronous Delay instead of Thread.Sleep. First significant change in behavior!
1. Making a non-blocking asynchronous chain with custom Delay and ContinueWith : First real, fully asynchronous solution
1. Making asynchronous chain with Standard Task class : Applying everything so far to the standard Task class
1. State Machine : Demonstrate a basic C-style state machine for reference
1. IEnumerable from the ground up : Demonstrate IEnumerable iterator as a specified form of previous state machine
1. IEnumerable iterator / yield return : Using compiler sugar to make iterators easy to write and read
1. Task iterator : Demonstrate iterating over Tasks returned by yield return to simulate async/await (Uses custom task class because TaskCompletionSource not introduced yet)
1. Custom await-able : Demonstrate how a task can become available to await -- first real use of async/await
1. Async/Await : First full, basic demonstration of async/await with standard Tasks only
1. TaskCompletionSource : Demonstrate how to represent synchronous or managed operations alongside asynchronous code via a custom managed Thread and TaskCompletionSource
1. CancellationToken : Demonstrate the use of cancellation tokens with asynchronous code. This fully brings everything so far into one sample.
1. Enumerable of Tasks : Get a basic feel for the logic behind IAsyncEnumerable
1. Custom IAsyncEnumerable implementation : Demonstrate implementing IAsyncEnumerable and IAsyncEnumerator and then using await foreach
1. IAsyncEnumerator iterator : Demonstrate using async/await and yield return together in an IAsyncEnumerator iterator method
1. Channels : Bring everything together in a simple demonstration of the Producer/Consumer asynchronous pattern in C# with Channels
