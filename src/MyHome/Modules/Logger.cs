using System;
using System.IO;

using MyHome.Constants;
using MyHome.Extensions;

namespace MyHome.Modules
{
    public class Logger
    {
        private class LogInstance
        {
            public IFileManager FileManager { get; set; }

            public FileStream FileStream { get; set; }

            public DateTime FileDate { get; set;  }

            public string FileLogPath { get; set; }

            public bool ConsoleLogsEnabled { get; set; }

            public bool FileLogsEnabled { get; set; }

            public int FileLogCounter { get; set; }

            public int FileLogFlushThreshold { get; set; }

            public object FileWriteMutex { get; set; }
        }

        private static LogInstance Instance { get; set; }

        private const string LogFileExtension = ".log";
        private const string DebugMessageTemplate = "{0}: {1}";
        private const string FileMessageTemplate = "{0}|{1}|{2}|{3}\r\n";

        private string _context;

        private Logger(string context)
        {
            _context = context;
        }

        public void Print(string messageTemplate, params object[] args)
        {
            if (Instance.ConsoleLogsEnabled)
            {
                var formatetdMessage = messageTemplate.Format(args);
                var logMessage = DebugMessageTemplate.Format(_context, formatetdMessage);
                Microsoft.SPOT.Debug.Print(logMessage);
            }
        }

        public void Debug(string messageTemplate, params object[] args)
        {
            Log("DBG", _context, messageTemplate.Format(args));
        }

        public void Error(string messageTemplate, params object[] args)
        {
            Log("ERR", _context, messageTemplate.Format(args));
        }

        public void Information(string messageTemplate, params object[] args)
        {
            Log("INF", _context, messageTemplate.Format(args));
        }

        public void Warning(string messageTemplate, params object[] args)
        {
            Log("WRN", _context, messageTemplate.Format(args));
        }

        public static void Initialise(IFileManager fm)
        {
            Instance = new LogInstance
            {
                FileManager = fm,
                ConsoleLogsEnabled = true,
                FileLogsEnabled = false,
                FileWriteMutex = new object()
            };
        }

        public static Logger ForContext(object caller)
        {
            return new Logger(caller.GetType().Name);
        }

        public static void SetupFileLogging(bool enabled)
        {
            Instance.FileLogsEnabled = false;
            lock (Instance.FileWriteMutex)
            {
                try 
                {
                    // Flush and close the existing log file
                    if (Instance.FileStream != null &&
                        Instance.FileStream.CanWrite)
                    {
                        LogInternal("Closing existing file log");
                        Instance.FileStream.Flush();
                        Instance.FileStream.Close();
                    }
                }
                catch
                {
                }

                // Start new log file
                if (enabled)
                {
                    LogInternal("Enabling file logging");
                    Instance.FileLogCounter = 0;
                    Instance.FileLogFlushThreshold = 50;
                    Instance.FileDate = DateTime.Today;
                    Instance.FileLogPath = Path.Combine(Directories.Logs, Instance.FileDate.Datestamp() + LogFileExtension);
                    Instance.FileStream = Instance.FileManager.GetFileStream(Instance.FileLogPath, FileMode.Append, FileAccess.Write);
                    Instance.FileLogsEnabled = true;
                }
                else
                {
                    LogInternal("Disabling file logging");
                }
            }
        }

        private static void LogInternal(string message)
        {
            var log = DebugMessageTemplate.Format("Logger", message);
            Microsoft.SPOT.Debug.Print(log);
        }

        private static void Log(string status, string context, string message)
        {
            var now = DateTime.Now;
            if (Instance.ConsoleLogsEnabled)
            {
                var debugLog = DebugMessageTemplate.Format(context, message);
                Microsoft.SPOT.Debug.Print(debugLog);
            }

            if (Instance.FileLogsEnabled)
            {
                var fileLog = FileMessageTemplate.Format(now.SortableDateTime(), status, context, message);
                var bytes = fileLog.GetBytes();

                if (Instance.FileDate < DateTime.Today)
                {
                    LogInternal("Generating new file for today");
                    SetupFileLogging(true);
                }

                lock (Instance.FileWriteMutex)
                {
                    Instance.FileStream.Write(bytes, 0, bytes.Length);
                    Instance.FileLogCounter++;

                    if (Instance.FileLogCounter > Instance.FileLogFlushThreshold)
                    {
                        LogInternal("Flushing logs to SD card");
                        Instance.FileStream.Flush();
                        Instance.FileLogCounter = 0;
                    }
                }
            }
        }
    }
}
