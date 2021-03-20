using System;
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
        private DateTime _deviceStartTime;
        private bool _syncronisingTime;
        private bool _isTimeSynchronised;

        public SystemManager()
        {
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

            Debug.Print("Attempting to synchronise time...");
            foreach (var hostname in TimeServers)
            {
                Debug.Print("Using server: " + hostname);
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
                    Debug.Print("Time synchronisation failed.");
                }
            }

            if (success)
            {
                // Recalculate the start time using the current known uptime
                _deviceStartTime = Time - (timeBeforeSync - _deviceStartTime);
                Debug.Print("Synchronised time: " + JsonConvert.SerializeObject(Time));
                Debug.Print("Recalculated uptime: " + JsonConvert.SerializeObject(Uptime));
            }
            else
            {
                Debug.Print("Synchronisation to all time servers failed.");   
            }

            _syncronisingTime = false;
            return _isTimeSynchronised;
        }

        private static bool GetTime(string hostname, ushort port, out DateTime datetime)
        {
            string timeStr = null;
            var networkThread = new Awaitable(() =>
            {
                timeStr = GetTimeString(hostname, port);
            });
            networkThread.Await(3000); // Timeout network request after 3 seconds

            if (!timeStr.IsNullOrEmpty() &&
                (hostname.Contains("nist") && DateTimeParser.NIST(timeStr, out datetime) ||
                 hostname.Contains("ntp") && DateTimeParser.NTP(timeStr, out datetime)))
            {
                // Convert UTC to GMT
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

        private static string GetTimeString(string hostname, ushort port)
        {
            SocketClient client = new SocketClient();
            try
            {
                client.ConnectSocket(hostname, port);
                string message;
                if (client.GetMessage(out message))
                {
                    return message;
                }
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                client.CloseConnection();
            }
            return string.Empty;
        }
    }
}
