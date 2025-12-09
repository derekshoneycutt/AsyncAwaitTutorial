/*
 * =====================================================
 *         Step 21 : IAsyncDisposable
 * 
 *  Now we take another small tangent to discuss IAsyncDisposable.
 *  We'll just create a fresh sample without copying prior code,
 *  and start with IDisposable, then add IAsyncDisposable on to it.
 *  
 *  A.  Starting fresh, create a simple MyDisposable class.
 *      For the first step, implement IDisposable with the
 *      disposable pattern. VS will do most of the work here for us.
 *      
 *  B.  Setup Run so that it will construct 2 of our disposables:
 *      The first will be a top-level using,
 *      the second a parenthesized using with a scoped code block.
 *      This shows the two different ways that disposables
 *      are handled with using. We also can just call Dispose directly.
 *      
 *  C.  Add IAsyncDisposable to the MyDisposable class and try to
 *      follow a similar disposable pattern, but with async
 *      code instead. We can call the original internal Dispose
 *      pattern with a false parameter after the async code
 *      to ensure some necessarily synchronous cleanup is shared.
 *      
 *  D.  Change the using statements in Run to await using.
 *      We see nothing has really changed, but it will now call
 *      DisposeAsync instead of Dispose.
 *      
 *      
 * Async disposables are an important and powerful tool for
 * managing asynchronous resources, allowing us to free
 * them as asynchronously as we are using them.
 * 
 * =====================================================
*/

namespace AsyncAwaitTutorial;

/// <summary>
/// Sample containing a demonstration of the IAsyncDisposable interface for disposing resource asynchronously
/// </summary>
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
        await using var _ = new MyDisposable("Disposable Major").ConfigureAwait(false);


        // This one will dispose at the end of the code block that immediately follows.
        await using (new MyDisposable("Disposable Minor").ConfigureAwait(false))
        {
            Console.WriteLine("Writing before dispose.");
        }


        Console.WriteLine("Writing after dispose.");
    }
}
