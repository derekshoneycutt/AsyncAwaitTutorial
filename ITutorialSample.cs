namespace AsyncAwaitTutorial;


/// <summary>
/// Interface describing a sample that can be run for the tutorial
/// </summary>
public interface ITutorialSample
{
    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    Task Run(
        CancellationToken cancellationToken);
}
