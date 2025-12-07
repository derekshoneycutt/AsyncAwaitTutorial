namespace AsyncAwaitTutorial;


/// <summary>
/// Sample containing a demonstration of the IAsyncDisposable interface for disposing resource asynchronously
/// </summary>
/// <remarks>
/// This one is a complete tangent with completely unrelated code. However, it is an important and useful
/// tool for an async programmer's toolbox, and we we demonstrate it here.
/// </remarks>
public class IAsyncDisposableSample : ITutorialSample
{
    /// <summary>
    /// Disposable class that writes to the console when it is disposing.
    /// </summary>
    public class MyDisposable(string identifier)
        : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The disposed value
        /// </summary>
        private bool disposedValue;

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // We should *not* run into this one!
                    Console.WriteLine($"Disposing managed resources. {identifier}");
                }

                Console.WriteLine($"Disposing unmanaged resources. {identifier}");
                disposedValue = true;
            }
        }

        /// <summary>
        /// Releases managed resources as an asynchronous operation.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous dispose operation.</returns>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (!disposedValue)
            {
                // We *should* run into this one!
                Console.WriteLine($"Disposing managed resources asynchronously. {identifier}");
            }
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }
    }



    /// <summary>
    /// Runs sample code for the sample.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to signal that a process should not complete.</param>
    public async Task Run(
        CancellationToken cancellationToken)
    {
        Console.WriteLine();

        // This one will dispose at the very end of the method
        await using var majorDisposable = new MyDisposable("Disposable Major").ConfigureAwait(false);


        // This one will dispose at the end of the code block that immediately follows.
        await using (var minorDisposable = new MyDisposable("Disposable Minor").ConfigureAwait(false))
        {
            Console.WriteLine("Writing before dispose.");
        }


        Console.WriteLine("Writing after dispose.");
    }
}
