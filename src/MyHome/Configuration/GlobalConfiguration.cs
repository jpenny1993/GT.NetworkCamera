using System;

namespace MyHome.Configuration
{
    public sealed class GlobalConfiguration
    {
        public AttendanceConfiguration Attendance;

        public CameraConfiguration Camera;

        public LightConfiguration Lights;

        public LoggerConfiguration Logging;

        public NetworkConfiguration Network;

        public SensorConfiguration Sensors;

        public static GlobalConfiguration DefaultConfiguration 
        { 
            get 
            {
                return new GlobalConfiguration
                {
                    Attendance = new AttendanceConfiguration
                    {
                        AllowNewUsers = true,
                        WorkingDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                        OpeningHours = new TimeSpan(9, 0, 0),
                        ClosingHours = new TimeSpan(17, 30, 0)
                    },
                    Camera = new CameraConfiguration
                    {
                        Enabled = true,
                        SavePicturesToSdCard = true,
                        DaysToTakePicturesOn = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                        TakePicturesFrom = new TimeSpan(10, 0, 0),
                        TakePicturesUntil = new TimeSpan(16, 00, 0)
                    },
                    Lights = new LightConfiguration
                    {
                        LEDsOnFrom = new TimeSpan(8, 30, 0),
                        LEDsOnUntil = new TimeSpan(19, 0, 0)
                    },
                    Logging = new LoggerConfiguration
                    {
                        ConsoleLogsEnabled = true,
                        FileLogsEnabled = false
                    },
                    Network = new NetworkConfiguration
                    {
                        UseDHCP = true,
                        IPAddress = "192.168.1.8",
                        SubnetMask = "255.255.255.0",
                        Gateway = "192.168.1.1"
                    },
                    Sensors = new SensorConfiguration
                    {
                        SaveMeasurementsToSdCard = true
                    }
                };
            } 
        }
    }
}
