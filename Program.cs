
using AsyncAwaitTutorial;

Dictionary<int, TutorialSample> samples = new()
{
    { 1, new("Baseline Procedural", "Gives the basic idea in a synchronous, procedural matter, without even using threads.", new ProceduralSample()) },
    { 2, new("List Producer", "Demonstrates separating value production into a second method.", new ListProducerSample()) },
    { 3, new("State Machines", "Demonstrates a basic C-style state machine for reference.", new StateMachineProducerSample()) },
    { 4, new("IEnumerable from the ground up", "Demonstrates IEnumerable iterator as a specified form of previous state machine.", new IEnumerableProducerSample()) },
    { 5, new("Iterator / yield return", "Uses compiler sugar to make iterators easy to write and read.", new IteratorProducerSample()) },
    { 6, new("Threads", "Opens several threads to demonstrate basic concurrency.", new ThreadSample()) },
    { 7, new("Custom Thread Pool", "Introduces the basic idea of a thread pool with a couple of reusable threads.", new MyThreadPoolSample()) },
    { 8, new("Custom Thread Pool with Execution Context", "Utilizes ExecutionContext to maintain appropriate thread local storage across a thread pool.", new MyThreadPoolWithContextSample()) },
    { 9, new("ThreadPool", "Uses the built in C# ThreadPool instead of a custom one.", new ThreadPoolSample()) },
    { 10, new("Custom Task Completion", "Demonstrates the logic behind Task with a custom Task Completion class as the first step.", new MyTaskCompletionSample()) },
    { 11, new("Custom Task", "Demonstrates the logic behind Task with a custom task class.", new MyTaskSample()) },
    { 12, new("Custom WhenAll", "Demonstrates how WhenAll is created via custom task.", new MyTaskWhenAllSample()) },
    { 13, new("Custom Delay", "Creates and uses an asynchronous Delay instead of Thread.Sleep.", new MyTaskDelaySample()) },
    { 14, new("Asynchronous chain with custom Task", "First real, fully asynchronous solution.", new MyTaskAsyncChainsSample()) },
    { 15, new("Task iterator", "Demonstrates iterating over Tasks returned by yield return to simulate async/await (Uses custom task class because TaskCompletionSource not introduced yet).", new IterateTaskGeneratorSample()) },
    { 16, new("Custom await-able", "Demonstrates how a task can become available to await -- first real use of async/await.", new AwaitableCustomSample()) },
    { 17, new("Async/Await", "First full, basic demonstration of async/await with standard Tasks only.", new StdAwaitSample()) },
    { 18, new("TaskCompletionSource", "Demonstrates how to represent synchronous or managed operations alongside asynchronous code via a custom managed Thread and TaskCompletionSource.", new TaskCompletionSourceSample()) },
    { 19, new("Custom CancellationToken", "Demonstrates the structure of cancellation tokens via a custom cancellation token source class.", new MyCancellationTokenSample()) },
    { 20, new("IAsyncDisposable", "Demonstrates the use of IAsyncDisposable and await using for asynchronous disposal of resources.", new IAsyncDisposableSample()) },
    { 21, new("CancellationToken", "Demonstrates the use of cancellation tokens with asynchronous code. This fully brings everything so far into one sample.", new CancellationTokenSample()) },
    { 22, new("Custom IAsyncEnumerable implementation", "Demonstrates implementing IAsyncEnumerable and IAsyncEnumerator and then using await foreach.", new CustomAsyncEnumerableSample()) },
    { 23, new("IAsyncEnumerable iterator with await and yield return", "Demonstrates using async/await and yield return together in an IAsyncEnumerator iterator method.", new IAsyncEnumerableGeneratorSample()) },
    { 24, new("Custom Channels", "Extends the previous work into a basic implementation of Channels for Producer/Consumer pattern.", new MyChannelSample()) },
    { 25, new("Channels", "Simple demonstration of the Producer/Consumer asynchronous pattern in C# with Channels.", new ChannelsSample()) },
    { 26, new("Structured Channels", "Demonstration of a structured use of channels for streaming values.", new StructuredChannelsSample()) },
    { 27, new("Channel Middleman", "Demonstration of adding a middleman to the structured channels pipeline.", new ChannelMiddlemanSample()) },
    { 28, new("DataFlow Middleman", "Turns the middleman of the channels pipeline into a DataFlow pipeline.", new DataFlowMiddlemanSample()) },
    { 29, new("DataFlow Complete", "Turns the entire pipeline into a DataFlow pipeline.", new DataFlowCompleteSample()) }
};

bool doContinue = true;

while (doContinue)
{
    Console.WriteLine("Please select the test to run:");
    Console.WriteLine();
    foreach (KeyValuePair<int, TutorialSample> kvp in samples)
    {
        Console.WriteLine($"    {kvp.Key}. {kvp.Value.Name}");
    }
    Console.WriteLine();
    Console.WriteLine("Enter \"exit\" to exit. Enter \"help\" for description of each option.");
    Console.Write("Enter your selection as a number (or exit/help) and press Enter: ");

    string? entry = Console.ReadLine();

    while (String.Equals(entry, "help", StringComparison.CurrentCultureIgnoreCase))
    {
        Console.WriteLine();
        foreach (KeyValuePair<int, TutorialSample> kvp in samples)
        {
            Console.WriteLine($"    {kvp.Key}. {kvp.Value.Name}: {kvp.Value.Help}");
        }
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
            if (samples.TryGetValue(selection, out TutorialSample? sample) && (sample is not null))
            {
                await sample.Sample.Run(cts.Token).ConfigureAwait(false);
            }
            else
            {

                Console.WriteLine("Unrecognized entry. Try again.");
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


record TutorialSample(
    string Name, string Help, ITutorialSample Sample);