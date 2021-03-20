using System;

namespace MyHome.Modules
{
    public interface ISystemManager
    {
        bool IsTimeSynchronised { get; }
        DateTime StartTime { get; }
        DateTime Time { get; }
        TimeSpan Uptime { get; }

        bool SyncroniseInternetTime();
    }
}
