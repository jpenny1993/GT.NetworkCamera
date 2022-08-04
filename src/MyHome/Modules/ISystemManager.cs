using System;

namespace MyHome.Modules
{
    public interface ISystemManager
    {
        bool HasTimeSyncronised { get; }
        DateTime StartTime { get; }
        DateTime Time { get; }
        DateTime UtcTime { get; }
        TimeSpan Uptime { get; }

        event SystemManager.TimeSynchronised OnTimeSynchronised;

        bool SyncroniseInternetTime();
    }
}
