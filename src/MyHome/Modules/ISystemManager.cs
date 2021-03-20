using System;

namespace MyHome.Modules
{
    public interface ISystemManager
    {
        void SetSystemStartTime(DateTime time);
        DateTime StartTime { get; }
        DateTime Time { get; }
        TimeSpan Uptime { get; }
    }
}
