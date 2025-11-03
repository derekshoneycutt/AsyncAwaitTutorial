
using AsyncAwaitTutorial;
using System.CommandLine;

RootCommand rootCommand = new(description: "Async/Await Tutorial Samples Application");


Command runner = new("run", "Demonstrate running a sample.");
Option<int> numberedOpt = new("--numbered")
{
    Description = "Run a numbered sample.",
    DefaultValueFactory = _ => -1
};
runner.Options.Add(numberedOpt);
runner.SetAction(async (results, cancellationToken) =>
{
    switch (results.GetValue<int>(numberedOpt))
    {
        case 1:
            ThreadSamples.Run(results);
            break;
        case 2:
            CustomThreadPoolSamples.Run(results);
            break;
        case 3:
            ContextedCustomThreadPoolSamples.Run(results);
            break;
        case 4:
            ThreadPoolSamples.Run(results);
            break;
        case 5:
            CustomTaskSamples.Run(results);
            break;
        case 6:
            CustomTaskWhenAllSamples.Run(results);
            break;
        case 7:
            CustomTaskDelaySamples.Run(results);
            break;
        case 8:
            CustomTaskDelayNonBlockingSamples.Run(results);
            break;
        case 9:
            StdTaskAtContinueWithSample.Run(results);
            break;
        case 10:
            StateMachineSample.Run(results);
            break;
        case 11:
            IEnumerableSample.Run(results);
            break;
        case 12:
            IEnumerableGeneratorSample.Run(results);
            break;
        case 13:
            IterateTaskGeneratorSample.Run(results);
            break;
        case 14:
            await AwaitableCustomSample.Run(results, cancellationToken);
            break;
        case 15:
            await StdAwaitSample.Run(results, cancellationToken);
            break;
        case 16:
            await TaskCompletionSourceSample.Run(results, cancellationToken);
            break;
        case 17:
            await CancellationTokenSample.Run(results, cancellationToken);
            break;
        case 18:
            await EnumerableOfTasksSample.Run(results, cancellationToken);
            break;
        case 19:
            await CustomAsyncEnumerableSample.Run(results, cancellationToken);
            break;
        case 20:
            await IAsyncEnumerableGeneratorSample.Run(results, cancellationToken);
            break;
        case 21:
            await ChannelsSample.Run(results, cancellationToken);
            break;

        default:
            Console.WriteLine("Number requested is not recognized or not entered.");
            return 1;
    }

    return 0;
});
rootCommand.Subcommands.Add(runner);

Command thread = new("thread", "Demonstrate running a thread.");
thread.SetAction(ThreadSamples.Run);
rootCommand.Subcommands.Add(thread);



Command customThreadPool = new("customThreadPool", "Demonstrates a custom thread pool.");
customThreadPool.SetAction(CustomThreadPoolSamples.Run);
rootCommand.Subcommands.Add(customThreadPool);



Command contextedCustomThreadPool = new("contextedCustomThreadPool", "Demonstrates a custom thread pool utilizing contexts.");
contextedCustomThreadPool.SetAction(ContextedCustomThreadPoolSamples.Run);
rootCommand.Subcommands.Add(contextedCustomThreadPool);



Command threadPool = new("threadPool", "Demonstrates a custom thread pool.");
threadPool.SetAction(ThreadPoolSamples.Run);
rootCommand.Subcommands.Add(threadPool);



Command customTask = new("customTask", "Demonstrates a custom task object.");
customTask.SetAction(CustomTaskSamples.Run);
rootCommand.Subcommands.Add(customTask);



Command customTaskWhenAll = new("customTaskWhenAll", "Demonstrates a custom task object using WhenAll.");
customTaskWhenAll.SetAction(CustomTaskWhenAllSamples.Run);
rootCommand.Subcommands.Add(customTaskWhenAll);



Command customTaskWithDelay = new("customTaskWithDelay", "Demonstrates a custom task object with a custom Delay method.");
customTaskWithDelay.SetAction(CustomTaskDelaySamples.Run);
rootCommand.Subcommands.Add(customTaskWithDelay);



Command customTaskWithDelayNonBlocking = new("customTaskWithDelayNonBlocking", "Demonstrates a custom task object with a custom Delay method in a non-blocking fashion.");
customTaskWithDelayNonBlocking.SetAction(CustomTaskDelayNonBlockingSamples.Run);
rootCommand.Subcommands.Add(customTaskWithDelayNonBlocking);



Command stdTaskAtContinueWith = new("stdTaskAtContinueWith", "Demonstrates a standard task object with Delay method in a non-blocking fashion.");
stdTaskAtContinueWith.SetAction(StdTaskAtContinueWithSample.Run);
rootCommand.Subcommands.Add(stdTaskAtContinueWith);


Command basicStateMachine = new("basicStateMachine", "Demonstrate running a basic state machine.");
basicStateMachine.SetAction(StateMachineSample.Run);
rootCommand.Subcommands.Add(basicStateMachine);


Command ienumerable = new("ienumerable", "Demonstrate running an IEnumerable state machine.");
ienumerable.SetAction(IEnumerableSample.Run);
rootCommand.Subcommands.Add(ienumerable);


Command ienumerableGenerator = new("ienumerableGenerator", "Demonstrate running an IEnumerable Generator Method state machine.");
ienumerableGenerator.SetAction(IEnumerableGeneratorSample.Run);
rootCommand.Subcommands.Add(ienumerableGenerator);


Command iterateTaskGenerator = new("iterateTaskGenerator", "Demonstrate running using an iterator to simulate async/await with generators and custom task type.");
iterateTaskGenerator.SetAction(IterateTaskGeneratorSample.Run);
rootCommand.Subcommands.Add(iterateTaskGenerator);


Command asyncCustom = new("asyncCustom", "Demonstrate running using async/await with the custom task type.");
asyncCustom.SetAction(AwaitableCustomSample.Run);
rootCommand.Subcommands.Add(asyncCustom);


Command stdAwait = new("stdAwait", "Demonstrate running using async/await with the standard task type.");
stdAwait.SetAction(StdAwaitSample.Run);
rootCommand.Subcommands.Add(stdAwait);


Command taskCompletionSource = new("taskCompletionSource", "Demonstrate running using async/await with the standard task type plus a TaskCompletionSource for an additional thread.");
taskCompletionSource.SetAction(TaskCompletionSourceSample.Run);
rootCommand.Subcommands.Add(taskCompletionSource);


Command cancellationToken = new("cancellationToken", "Demonstrate running using async/await with the standard task type plus CancellationTokens.");
cancellationToken.SetAction(CancellationTokenSample.Run);
rootCommand.Subcommands.Add(cancellationToken);


Command enumerableOfTasks = new("enumerableOfTasks", "Demonstrate a situation involving an enumerable of tasks.");
enumerableOfTasks.SetAction(EnumerableOfTasksSample.Run);
rootCommand.Subcommands.Add(enumerableOfTasks);


Command customAsyncEnumerable = new("customAsyncEnumerable", "Demonstrate a custom IAsyncEnumerable implementation.");
customAsyncEnumerable.SetAction(CustomAsyncEnumerableSample.Run);
rootCommand.Subcommands.Add(customAsyncEnumerable);



Command stdAsyncEnumerable = new("stdAsyncEnumerable", "Demonstrate a standard generator IAsyncEnumerable implementation.");
stdAsyncEnumerable.SetAction(IAsyncEnumerableGeneratorSample.Run);
rootCommand.Subcommands.Add(stdAsyncEnumerable);


Command channels = new("channels", "Demonstrate the use of a channel in C#.");
channels.SetAction(ChannelsSample.Run);
rootCommand.Subcommands.Add(channels);


ParseResult parseResult = rootCommand.Parse(args);
int retValue = await parseResult.InvokeAsync().ConfigureAwait(false);

Console.WriteLine("Press any key to exit");
Console.ReadKey();

return retValue;
