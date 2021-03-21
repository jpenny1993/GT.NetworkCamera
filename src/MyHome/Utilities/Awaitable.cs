using System;
using System.Threading;

namespace MyHome.Utilities
{
    public sealed class Awaitable : IAwaitable, IDisposable
    {
        public static IAwaitable Default { get { return new Awaitable(); } }

        private readonly Thread _thread;
        private bool _disposed;
        private bool _isRunning;

        private Awaitable()
        {
        }

        public Awaitable(ThreadStart action)
        {
            _isRunning = true;
            _thread = new Thread(() =>
            {
                action.Invoke();
                _isRunning = false;
            });
            _thread.Start();
        }

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public void Await()
        {
            while (_isRunning)
            {
                Thread.Sleep(1000);
            }
        }

        public void Await(int timeoutMs)
        {
            var expires = new TimeSpan(0, 0, 0, 0, timeoutMs);
            var time = DateTime.Now;
            while (_isRunning)
            {
                var timespan = DateTime.Now - time;
                if (timespan > expires)
                {
                    Dispose();
                    break;
                }

                Thread.Sleep(1000);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                DisposeInternal();
            }
        }

        private void DisposeInternal() 
        {
            if (_thread != null && _thread.IsAlive)
            {
                try
                {
                    _thread.Abort();
                }
                catch
                { 
                }
            }
            _isRunning = false;
        }
    }
}
