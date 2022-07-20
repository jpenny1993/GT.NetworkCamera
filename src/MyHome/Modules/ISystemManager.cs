﻿using System;

namespace MyHome.Modules
{
    public interface ISystemManager
    {
        DateTime StartTime { get; }
        DateTime Time { get; }
        TimeSpan Uptime { get; }

        event SystemManager.TimeSynchronised OnTimeSynchronised;

        bool SyncroniseInternetTime();
    }
}
