
using AsyncAwaitTutorial;


bool doContinue = true;

while (doContinue)
{
    Console.WriteLine("Please select the test to run:");
    Console.WriteLine();
    Console.WriteLine("    1. Threads");
    Console.WriteLine("    2. Custom Thread Pool");
    Console.WriteLine("    3. Custom Thread Pool with Execution Context");
    Console.WriteLine("    4. TheadPool");
    Console.WriteLine("    5. Custom Task");
    Console.WriteLine("    6. Custom WhenAll");
    Console.WriteLine("    7. Custom Delay");
    Console.WriteLine("    8. Asynchronous chain with custom Task");
    Console.WriteLine("    9. Asynchronous chain with standard Task");
    Console.WriteLine("    10. State Machines");
    Console.WriteLine("    11. IEnumerable from the ground up");
    Console.WriteLine("    12. Iterator / yield return");
    Console.WriteLine("    13. Task iterator");
    Console.WriteLine("    14. Custom await-able");
    Console.WriteLine("    15. Async/Await");
    Console.WriteLine("    16. TaskCompletionSource");
    Console.WriteLine("    17. CancellationToken");
    Console.WriteLine("    18. Enumerable of Tasks");
    Console.WriteLine("    19. Custom IAsyncEnumerable implementation");
    Console.WriteLine("    20. IAsyncEnumerable iterator with await and yield return");
    Console.WriteLine("    21. Channels");
    Console.WriteLine();
    Console.WriteLine("Enter \"exit\" to exit. Enter \"help\" for description of each option.");
    Console.Write("Enter your selection and press Enter: ");

    string? entry = Console.ReadLine();

    while (String.Equals(entry, "help", StringComparison.CurrentCultureIgnoreCase))
    {
        Console.WriteLine();
        Console.WriteLine("    1. Threads: Opens several threads to demonstrate basic concurrency");
        Console.WriteLine("    2. Custom Thread Pool: Introduces the basic idea of a thread pool with a couple of reusable threads");
        Console.WriteLine("    3. Custom Thread Pool with Execution Context: Utilizes ExecutionContext to maintain appropriate thread local storage across a thread pool");
        Console.WriteLine("    4. TheadPool: Uses the built in C# ThreadPool instead of a custom one");
        Console.WriteLine("    5. Custom Task: Demonstrates the logic behind Task with a custom task class");
        Console.WriteLine("    6. Custom WhenAll: Demonstrates how WhenAll is created via custom task");
        Console.WriteLine("    7. Custom Delay: Creates and uses an asynchronous Delay instead of Thread.Sleep. First significant change in behavior!");
        Console.WriteLine("    8. Asynchronous chain with custom Task: First real, fully asynchronous solution");
        Console.WriteLine("    9. Asynchronous chain with standard Task: Applies everything so far to the standard Task class");
        Console.WriteLine("    10. State Machines: Demonstrates a basic C-style state machine for reference");
        Console.WriteLine("    11. IEnumerable from the ground up: Demonstrates IEnumerable iterator as a specified form of previous state machine");
        Console.WriteLine("    12. Iterator / yield return: Uses compiler sugar to make iterators easy to write and read");
        Console.WriteLine("    13. Task iterator: Demonstrates iterating over Tasks returned by yield return to simulate async/await (Uses custom task class because TaskCompletionSource not introduced yet)");
        Console.WriteLine("    14. Custom await-able: Demonstrates how a task can become available to await -- first real use of async/await");
        Console.WriteLine("    15. Async/Await: First full, basic demonstration of async/await with standard Tasks only");
        Console.WriteLine("    16. TaskCompletionSource: Demonstrates how to represent synchronous or managed operations alongside asynchronous code via a custom managed Thread and TaskCompletionSource");
        Console.WriteLine("    17. CancellationToken: Demonstrates the use of cancellation tokens with asynchronous code. This fully brings everything so far into one sample.");
        Console.WriteLine("    18. Enumerable of Tasks: Gets a basic feel for the logic behind IAsyncEnumerable");
        Console.WriteLine("    19. Custom IAsyncEnumerable implementation: Demonstrates implementing IAsyncEnumerable and IAsyncEnumerator and then using await foreach");
        Console.WriteLine("    20. IAsyncEnumerable iterator with await and yield return: Demonstrates using async/await and yield return together in an IAsyncEnumerator iterator method");
        Console.WriteLine("    21. Channels: Brings everything together in a simple demonstration of the Producer/Consumer asynchronous pattern in C# with Channels");
        Console.WriteLine();
        Console.WriteLine("Enter \"exit\" to exit. Enter \"help\" for description of each option.");
        Console.Write("Enter your selection and press Enter: ");

        entry = Console.ReadLine();
    }

    if (String.Equals(entry, "exit", StringComparison.CurrentCultureIgnoreCase))
    {
        doContinue = false;
    }
    else if (Int32.TryParse(entry, out int selection))
    {
        CancellationTokenSource cts = new(15000);

        switch (selection)
        {
            case 1:
                ThreadSamples.Run();
                break;
            case 2:
                MyThreadPoolSamples.Run();
                break;
            case 3:
                MyThreadPoolWithContextSamples.Run();
                break;
            case 4:
                ThreadPoolSamples.Run();
                break;
            case 5:
                MyTaskSamples.Run();
                break;
            case 6:
                MyTaskWhenAllSamples.Run();
                break;
            case 7:
                MyTaskDelaySamples.Run();
                break;
            case 8:
                MyTaskAsyncChainSamples.Run();
                break;
            case 9:
                StdTaskAtContinueWithSample.Run();
                break;
            case 10:
                StateMachineSample.Run();
                break;
            case 11:
                IEnumerableSample.Run();
                break;
            case 12:
                IteratorSample.Run();
                break;
            case 13:
                IterateTaskGeneratorSample.Run();
                break;
            case 14:
                await AwaitableCustomSample.Run();
                break;
            case 15:
                await StdAwaitSample.Run();
                break;
            case 16:
                await TaskCompletionSourceSample.Run();
                break;
            case 17:
                await CancellationTokenSample.Run(cts.Token);
                break;
            case 18:
                await EnumerableOfTasksSample.Run(cts.Token);
                break;
            case 19:
                await CustomAsyncEnumerableSample.Run(cts.Token);
                break;
            case 20:
                await IAsyncEnumerableGeneratorSample.Run(cts.Token);
                break;
            case 21:
                await ChannelsSample.Run(cts.Token);
                break;

            default:
                Console.WriteLine("Unrecognized entry. Try again.");
                break;
        }
    }
    else
    {

        Console.WriteLine("Unrecognized entry. Try again.");
    }
}


return 0;