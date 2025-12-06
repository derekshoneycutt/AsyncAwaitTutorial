
using AsyncAwaitTutorial;


bool doContinue = true;

while (doContinue)
{
    Console.WriteLine("Please select the test to run:");
    Console.WriteLine();
    Console.WriteLine("    1. Baseline Procedural");
    Console.WriteLine("    2. Threads");
    Console.WriteLine("    3. Custom Thread Pool");
    Console.WriteLine("    4. Custom Thread Pool with Execution Context");
    Console.WriteLine("    5. ThreadPool");
    Console.WriteLine("    6. Custom Task Completion");
    Console.WriteLine("    7. Custom Task");
    Console.WriteLine("    8. Custom WhenAll");
    Console.WriteLine("    9. Custom Delay");
    Console.WriteLine("    10. Asynchronous chain with custom Task");
    Console.WriteLine("    11. Asynchronous chain with standard Task");
    Console.WriteLine("    12. State Machines");
    Console.WriteLine("    13. IEnumerable from the ground up");
    Console.WriteLine("    14. Iterator / yield return");
    Console.WriteLine("    15. Task iterator");
    Console.WriteLine("    16. Custom await-able");
    Console.WriteLine("    17. Async/Await");
    Console.WriteLine("    18. TaskCompletionSource");
    Console.WriteLine("    19. MyCancellationToken");
    Console.WriteLine("    20. CancellationToken");
    Console.WriteLine("    21. IAsyncDisposable");
    Console.WriteLine("    22. Semaphore");
    Console.WriteLine("    23. Enumerable of Tasks");
    Console.WriteLine("    24. Custom IAsyncEnumerable implementation");
    Console.WriteLine("    25. IAsyncEnumerable iterator with await and yield return");
    Console.WriteLine("    26. Custom Channels");
    Console.WriteLine("    27. Channels");
    Console.WriteLine("    28. Structured Channels");
    Console.WriteLine("    29. IObservable");
    Console.WriteLine("    30. IAsyncObservable");
    Console.WriteLine();
    Console.WriteLine("Enter \"exit\" to exit. Enter \"help\" for description of each option.");
    Console.Write("Enter your selection as a number (or exit/help) and press Enter: ");

    string? entry = Console.ReadLine();

    while (String.Equals(entry, "help", StringComparison.CurrentCultureIgnoreCase))
    {
        Console.WriteLine();
        Console.WriteLine("    1. Baseline Procedural: Gives the basic idea in a synchronous, procedural matter, without even using threads.");
        Console.WriteLine("    2. Threads: Opens several threads to demonstrate basic concurrency");
        Console.WriteLine("    3. Custom Thread Pool: Introduces the basic idea of a thread pool with a couple of reusable threads");
        Console.WriteLine("    4. Custom Thread Pool with Execution Context: Utilizes ExecutionContext to maintain appropriate thread local storage across a thread pool");
        Console.WriteLine("    5. ThreadPool: Uses the built in C# ThreadPool instead of a custom one");
        Console.WriteLine("    6. Custom Task Completion: Demonstrates the logic behind Task with a custom Task Completion class as the first step.");
        Console.WriteLine("    7. Custom Task: Demonstrates the logic behind Task with a custom task class");
        Console.WriteLine("    8. Custom WhenAll: Demonstrates how WhenAll is created via custom task");
        Console.WriteLine("    9. Custom Delay: Creates and uses an asynchronous Delay instead of Thread.Sleep. First significant change in behavior!");
        Console.WriteLine("    10. Asynchronous chain with custom Task: First real, fully asynchronous solution");
        Console.WriteLine("    11. Asynchronous chain with standard Task: Applies everything so far to the standard Task class");
        Console.WriteLine("    12. State Machines: Demonstrates a basic C-style state machine for reference");
        Console.WriteLine("    13. IEnumerable from the ground up: Demonstrates IEnumerable iterator as a specified form of previous state machine");
        Console.WriteLine("    14. Iterator / yield return: Uses compiler sugar to make iterators easy to write and read");
        Console.WriteLine("    15. Task iterator: Demonstrates iterating over Tasks returned by yield return to simulate async/await (Uses custom task class because TaskCompletionSource not introduced yet)");
        Console.WriteLine("    16. Custom await-able: Demonstrates how a task can become available to await -- first real use of async/await");
        Console.WriteLine("    17. Async/Await: First full, basic demonstration of async/await with standard Tasks only");
        Console.WriteLine("    18. TaskCompletionSource: Demonstrates how to represent synchronous or managed operations alongside asynchronous code via a custom managed Thread and TaskCompletionSource");
        Console.WriteLine("    19. MyCancellationToken: Demonstrates the structure of cancellation tokens via a custom cancellation token source class.");
        Console.WriteLine("    20. CancellationToken: Demonstrates the use of cancellation tokens with asynchronous code. This fully brings everything so far into one sample.");
        Console.WriteLine("    21. IAsyncDisposable: Demonstrates the use of IAsyncDisposable and await using for asynchronous disposal of resources.");
        Console.WriteLine("    22. Semaphore: Demonstrates using semaphores to coordinate asynchronous code.");
        Console.WriteLine("    23. Enumerable of Tasks: Gets a basic feel for the logic behind IAsyncEnumerable");
        Console.WriteLine("    24. Custom IAsyncEnumerable implementation: Demonstrates implementing IAsyncEnumerable and IAsyncEnumerator and then using await foreach");
        Console.WriteLine("    25. IAsyncEnumerable iterator with await and yield return: Demonstrates using async/await and yield return together in an IAsyncEnumerator iterator method");
        Console.WriteLine("    26. Custom Channels: Extends the previous work into a basic implementation of Channels for Producer/Consumer pattern.");
        Console.WriteLine("    27. Channels: Simple demonstration of the Producer/Consumer asynchronous pattern in C# with Channels");
        Console.WriteLine("    28. Structured Channels: Demonstration of a structured use of channels for streaming values.");
        Console.WriteLine("    29. IObservable: Demonstration of wrapping IAsyncEnumerable from Channels with IObservable, pulling everything in the sequence in.");
        Console.WriteLine("    30. IAsyncObservable: Extends the IObservable sample with new IAsyncObservable and IAsyncObserver interfaces that use asynchronous notifications.");
        Console.WriteLine();
        Console.WriteLine("Enter \"exit\" to exit. Enter \"help\" for description of each option.");
        Console.Write("Enter your selection as a number (or exit/help) and press Enter: ");

        entry = Console.ReadLine();
    }

    if (String.Equals(entry, "exit", StringComparison.CurrentCultureIgnoreCase))
    {
        doContinue = false;
    }
    else if (Int32.TryParse(entry, out int selection))
    {
        CancellationTokenSource cts = new(15000);

        try
        {
            switch (selection)
            {
                case 1:
                    ProceduralSamples.Run();
                    break;
                case 2:
                    ThreadSamples.Run();
                    break;
                case 3:
                    MyThreadPoolSamples.Run();
                    break;
                case 4:
                    MyThreadPoolWithContextSamples.Run();
                    break;
                case 5:
                    ThreadPoolSamples.Run();
                    break;
                case 6:
                    MyTaskCompletionSamples.Run();
                    break;
                case 7:
                    MyTaskSamples.Run();
                    break;
                case 8:
                    MyTaskWhenAllSamples.Run();
                    break;
                case 9:
                    MyTaskDelaySamples.Run();
                    break;
                case 10:
                    MyTaskAsyncChainSamples.Run();
                    break;
                case 11:
                    StdTaskAtContinueWithSample.Run();
                    break;
                case 12:
                    StateMachineSample.Run();
                    break;
                case 13:
                    IEnumerableSample.Run();
                    break;
                case 14:
                    IteratorSample.Run();
                    break;
                case 15:
                    IterateTaskGeneratorSample.Run();
                    break;
                case 16:
                    await AwaitableCustomSample.Run();
                    break;
                case 17:
                    await StdAwaitSample.Run();
                    break;
                case 18:
                    await TaskCompletionSourceSample.Run();
                    break;
                case 19:
                    await MyCancellationTokenSample.Run();
                    break;
                case 20:
                    await CancellationTokenSample.Run(cts.Token);
                    break;
                case 21:
                    await IAsyncDisposableSample.Run(cts.Token);
                    break;
                case 22:
                    await SemaphoreSample.Run(cts.Token);
                    break;
                case 23:
                    await EnumerableOfTasksSample.Run(cts.Token);
                    break;
                case 24:
                    await CustomAsyncEnumerableSample.Run(cts.Token);
                    break;
                case 25:
                    await IAsyncEnumerableGeneratorSample.Run(cts.Token);
                    break;
                case 26:
                    await MyChannelSample.Run(cts.Token);
                    break;
                case 27:
                    await ChannelsSample.Run(cts.Token);
                    break;
                case 28:
                    await StructuredChannelsSample.Run(cts.Token);
                    break;
                case 29:
                    await IObservableSample.Run(cts.Token);
                    break;
                case 30:
                    await IAsyncObservableSample.Run(cts.Token);
                    break;

                default:
                    Console.WriteLine("Unrecognized entry. Try again.");
                    break;
            }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            Console.WriteLine("Operation cancelled due to timeout.");
        }
        finally
        {
            cts.Dispose();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }
    }
    else
    {

        Console.WriteLine("Unrecognized entry. Try again.");
    }
}


return 0;