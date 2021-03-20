using System;
namespace MyHome.Utilities
{
    public interface IAwaitable
    {
        void Await();
        void Await(int timeoutMs);
        bool IsRunning { get; }
    }
}
