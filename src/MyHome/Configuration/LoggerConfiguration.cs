using System;
using System.Ext.Xml;
using System.Text;
using System.Xml;
using Json.Lite;
using MyHome.Extensions;

namespace MyHome.Configuration
{
    public sealed class LoggerConfiguration
    {
        /// <summary>
        /// Print logs to debugger output when <see langword="true" />.
        /// </summary>
        public bool ConsoleLogsEnabled;

        /// <summary>
        /// Write logs to file on the SD card when <see langword="true" />.
        /// </summary>
        public bool FileLogsEnabled;

        public static LoggerConfiguration Read(XmlReader reader)
        {
            reader.Read(); // <Logging>

            var model = new LoggerConfiguration
            {
                ConsoleLogsEnabled = reader.ReadXmlElement().Validate("ConsoleLogsEnabled").GetBoolean(),
                FileLogsEnabled = reader.ReadXmlElement().Validate("FileLogsEnabled").GetBoolean(),
            };

            reader.Read(); // </Logging>

            return model;
        }

        public static void Write(XmlWriter writer, LoggerConfiguration logger)
        {
            writer.WriteStartElement("Logging");

            writer.WriteStartElement("ConsoleLogsEnabled");
            writer.WriteString(new StringBuilder().WriteBoolean(logger.ConsoleLogsEnabled).ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("FileLogsEnabled");
            writer.WriteString(new StringBuilder().WriteBoolean(logger.FileLogsEnabled).ToString());
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}
