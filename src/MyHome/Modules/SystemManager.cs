using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using Json.Lite;
using MyHome.Extensions;
using MyHome.Utilities;

namespace MyHome.Modules
{
    public sealed class SystemManager : ISystemManager
    {
        private static readonly string[] TimeServers = { "time.nist.gov", "time-c.nist.gov", "0.uk.pool.ntp.org" };
        private readonly Logger _logger;
        private DateTime _deviceStartTime;
        private bool _syncronisingTime;
        private bool _isTimeSynchronised;

        public event SystemManager.TimeSynchronised OnTimeSynchronised;

        public delegate void TimeSynchronised(bool synchronised);

        public SystemManager()
        {
            _logger = Logger.ForContext(this);
            _deviceStartTime = DateTime.Now;
            _isTimeSynchronised = false;
            _syncronisingTime = false;
        }

        public bool IsTimeSynchronised
        {
            get { return _isTimeSynchronised; }
        }

        public DateTime StartTime
        {
            get { return _deviceStartTime; }
        }

        public DateTime Time
        {
            get { return DateTime.Now; }
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
            DateTime currentTime;
            bool success = false;

            _logger.Information("Attempting to synchronise time...");
            foreach (var hostname in TimeServers)
            {
                _logger.Information("Using server: {0}", hostname);
                if (GetTime(hostname, 13, out currentTime))
                {
                    if (Time != currentTime)
                    {
                        Utility.SetLocalTime(currentTime); // set the system time
                    }
                    _isTimeSynchronised = success = true;
                    break;
                }
                else
                {
                    _logger.Information("Time synchronisation failed");
                }
            }

            if (success)
            {
                // Recalculate the start time using the current known uptime
                _deviceStartTime = Time - (timeBeforeSync - _deviceStartTime);
                _logger.Information("Synchronised time: ", JsonConvert.SerializeObject(Time));
                _logger.Information("Recalculated uptime: ", JsonConvert.SerializeObject(Uptime));
            }
            else
            {
                _logger.Information("Synchronisation to all time servers failed");
            }

            if (OnTimeSynchronised != null)
            {
                OnTimeSynchronised.Invoke(success);
            }

            _syncronisingTime = false;
            return _isTimeSynchronised;
        }

        private static bool GetTime(string hostname, ushort port, out DateTime datetime)
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
                (hostname.Contains("nist") && DateTimeParser.NIST(timeStr, out datetime) ||
                 hostname.Contains("ntp") && DateTimeParser.NTP(timeStr, out datetime)))
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
