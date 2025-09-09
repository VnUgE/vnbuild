using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;

namespace VNLib.Tools.Build.Executor
{
    /// <summary>
    /// Represents a background task that can be awaited and cancelled
    /// </summary>
    /// <param name="job"></param>
    /// <param name="cts"></param>
    public sealed class BackgroundTask(Task job, CancellationTokenSource cts) : IDisposable
    {
        private bool disposedValue;

        ///<inheritdoc/>
        public TaskAwaiter GetAwaiter() => job.GetAwaiter();

        /// <summary>
        /// Stops the background task if it is still running
        /// </summary>
        public Task StopAsync() => cts.CancelAsync();

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cts.Dispose();
                    job.Dispose();
                }
                disposedValue = true;
            }
        }

        ///<inheritdoc/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
