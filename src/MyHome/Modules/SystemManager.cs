using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using Json.Lite;
using MyHome.Extensions;
using MyHome.Utilities;
using MyHome.Constants;

namespace MyHome.Modules
{
    public sealed class SystemManager : ISystemManager
    {
        private static readonly string[] NistTimeServers = {
                                                               "time.nist.gov", 
                                                               "time-a-b.nist.gov", "time-a-g.nist.gov", "time-a-wwv.nist.gov",
                                                               "time-b-b.nist.gov", "time-b-g.nist.gov", "time-b-wwv.nist.gov",
                                                               "time-c-b.nist.gov", "time-c-g.nist.gov", "time-c-wwv.nist.gov",
                                                               "time-d-b.nist.gov", "time-d-g.nist.gov", "time-d-wwv.nist.gov"
                                                           };
        private static readonly string[] NtpTimeServers = { "time.windows.com", "time.cloudflare.com", "time.google.com", "time.apple.com" };
        private readonly Logger _logger;
        private DateTime _deviceStartTime;
        private bool _syncronisingTime;

        public DateTime TimeLastSyncronised { get; private set; }

        public bool HasTimeSyncronised { get { return TimeLastSyncronised != DateTime.MinValue; } }

        public event SystemManager.TimeSynchronised OnTimeSynchronised;

        public delegate void TimeSynchronised(bool synchronised);

        public SystemManager()
        {
            TimeLastSyncronised = DateTime.MinValue;
            _logger = Logger.ForContext(this);
            _deviceStartTime = DateTime.Now;
            _syncronisingTime = false;
        }

        public DateTime StartTime
        {
            get { return _deviceStartTime; }
        }

        public DateTime Time
        {
            get { return DateTime.Now; }
        }

        public DateTime UtcTime 
        {
            get { return DateTime.UtcNow; }
        }

        public TimeSpan Uptime
        {
            get { return DateTime.Now - _deviceStartTime; }
        }

        public bool SyncroniseInternetTime()
        {
            if (_syncronisingTime) return false;

            _syncronisingTime = true;
            DateTime timeBeforeSync = Time;
            

            _logger.Information("Attempting to synchronise time...");
            bool success = UpdateTimeFromServerList(DateTypes.NIST, NistTimeServers) ||
                           UpdateTimeFromServerList(DateTypes.NTP, NtpTimeServers);

            if (success)
            {
                // Recalculate the start time using the current known uptime
                _deviceStartTime = Time - (timeBeforeSync - _deviceStartTime);
                _logger.Information("Synchronised time: {0}", JsonConvert.SerializeObject(Time));
                _logger.Information("Recalculated uptime: {0}", JsonConvert.SerializeObject(Uptime));
            }
            else
            {
                _logger.Information("Synchronisation to all time servers failed");
            }

            if (OnTimeSynchronised != null)
            {
                OnTimeSynchronised.Invoke(success);
                TimeLastSyncronised = Time;
            }

            _syncronisingTime = false;
            return success;
        }

        private void SetTime(DateTime currentTime)
        {
            if (Time != currentTime)
            {
                Utility.SetLocalTime(currentTime); // sets the system time
            }
        }

        private bool UpdateTimeFromServerList(string dateType, string[] serverList)
        {
            DateTime currentTime;
            foreach (var hostname in serverList)
            {
                _logger.Information("Using server: {0}", hostname);
                const int ntpPort = 13;
                if (GetTime(dateType, hostname, ntpPort, out currentTime))
                {
                    SetTime(currentTime);
                    return true;
                }
                else
                {
                    _logger.Information("Time synchronisation failed");
                }
            }

            return false;
        }

        private static bool GetTime(string dateType, string hostname, ushort port, out DateTime datetime)
        {
            string timeStr = null;
            var networkThread = new Awaitable(() =>
            {
                using (var client = new SocketClient())
                {
                    client.ConnectSocket(hostname, port);
                    while (client.Active && !client.GetMessage(out timeStr))
                    {
                        Thread.Sleep(100);
                    }
                }
            });
            networkThread.Await(3000); // Timeout network request after 3 seconds

            if (!timeStr.IsNullOrEmpty() &&
                (dateType == DateTypes.NIST && DateTimeParser.NIST(timeStr, out datetime) ||
                 dateType == DateTypes.NTP && DateTimeParser.NTP(timeStr, out datetime)))
            {
                // Convert UTC/GMT to BST
                if (datetime.IsBST())
                {
                    datetime = datetime.AddHours(1);
                }

                return true;
            }
            else
            {
                datetime = DateTime.MinValue;
            }

            return false;
        }
    }
}
