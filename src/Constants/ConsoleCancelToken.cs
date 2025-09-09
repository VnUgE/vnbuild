using System;
using System.Threading;

namespace VNLib.Tools.Build.Executor.Constants
{
    internal sealed class ConsoleCancelToken : IDisposable
    {
        private readonly CancellationTokenSource _cts;

        public CancellationToken Token => _cts.Token;

        public ConsoleCancelToken(CancellationToken existingToken)
        {
            Console.CancelKeyPress += OnCancel;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(existingToken);
        }

        private void OnCancel(object? sender, ConsoleCancelEventArgs e)
        {
            _cts.Cancel();
            e.Cancel = true;
        }

        public void Dispose()
        {
            //Unsubscribe from event
            Console.CancelKeyPress -= OnCancel;
            _cts.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}