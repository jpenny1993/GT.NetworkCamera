using System;

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
    }
}
