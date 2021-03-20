using System;
using Microsoft.SPOT;

namespace MyHome.Modules
{
    public sealed class SystemManager : ISystemManager
    {
        private DateTime _start;

        public SystemManager()
        {
            _start = DateTime.Now;
        }

        public DateTime StartTime
        {
            get { return _start; }
        }

        public DateTime Time
        {
            get { return DateTime.Now; }
        }

        public TimeSpan Uptime
        {
            get { return DateTime.Now - _start; }
        }

        public void SetSystemStartTime(DateTime time)
        {
            _start = time;
        }
    }
}
