using System;
using System.Ext.Xml;
using System.IO;
using System.Xml;

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
                        ClosingHours = new TimeSpan(17, 30, 0),
                        GracePeriodInMinutes = 30
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

        public static GlobalConfiguration Read(Stream fileStream)
        {
            var readerSettings = new XmlReaderSettings
            {
                IgnoreWhitespace = true,
                IgnoreComments = true
            };

            var reader = XmlReader.Create(fileStream, readerSettings);

            reader.Read(); // <xml/>
            reader.Read(); // <GlobalConfiguration>

            var configuration = new GlobalConfiguration
            {
                Attendance = AttendanceConfiguration.Read(reader),
                Camera = CameraConfiguration.Read(reader),
                Lights = LightConfiguration.Read(reader),
                Logging = LoggerConfiguration.Read(reader),
                Network = NetworkConfiguration.Read(reader),
                Sensors = SensorConfiguration.Read(reader),
            };

            reader.Read(); // </GlobalConfiguration>

            var eof = reader.EOF;

            return configuration;
        }

        public static void Write(Stream fileStream, GlobalConfiguration configuration)
        {
            var writer = XmlWriter.Create(fileStream);

            writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");
            writer.WriteStartElement("GlobalConfiguration");

            AttendanceConfiguration.Write(writer, configuration.Attendance);
            CameraConfiguration.Write(writer, configuration.Camera);
            LightConfiguration.Write(writer, configuration.Lights);
            LoggerConfiguration.Write(writer, configuration.Logging);
            NetworkConfiguration.Write(writer, configuration.Network);
            SensorConfiguration.Write(writer, configuration.Sensors);

            writer.WriteEndElement();
            writer.Flush();
            writer.Close();
        }
    }
}
