using System;
using System.IO;
using Microsoft.SPOT;

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

        private const string DebugMessageTemplate = "{0}: {1}";
        private const string FileMessageTemplate = "{0}|{1}|{2}|{3}";

        private string _context;

        private Logger(string context)
        {
            _context = context;
        }

        public void Error(string messageTemplate, params object[] args)
        {
            Log("ERR", _context, messageTemplate.Format(args));
        }

        public void Information(string messageTemplate, params object[] args)
        {
            Log("INF", _context, messageTemplate.Format(args));
        }

        public void Trace(string messageTemplate, params object[] args)
        {
            Log("TRACE", _context, messageTemplate.Format(args));
        }

        public void Warning(string messageTemplate, params object[] args)
        {
            Log("WARN", _context, messageTemplate.Format(args));
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

        public static Logger ForContext(string context)
        {
            return new Logger(context);
        }

        public static void SetupFileLogging(bool enabled)
        {
            Instance.FileLogsEnabled = false;
            lock (Instance.FileWriteMutex)
            {
                // Flush and clost existing log file
                if (Instance.FileStream != null &&
                        Instance.FileStream.CanWrite)
                {
                    Instance.FileStream.Flush();
                    Instance.FileStream.Close();
                }

                // Start new log file
                if (enabled)
                {
                    Instance.FileLogCounter = 0;
                    Instance.FileLogFlushThreshold = 50;
                    Instance.FileDate = DateTime.Today;
                    Instance.FileLogPath = Path.Combine(Directories.Logs, Instance.FileDate.ToString("yyyyMMdd.log"));
                    Instance.FileStream = Instance.FileManager.GetFileStream(Instance.FileLogPath, FileMode.Append, FileAccess.Write);
                    Instance.FileLogsEnabled = true;
                }
            }
        }

        private static void Log(string status, string context, string message)
        {
            var now = DateTime.Now;
            if (Instance.ConsoleLogsEnabled)
            {
                var debugLog = DebugMessageTemplate.Format(context, message);
                Debug.Print(debugLog);
            }

            if (Instance.FileLogsEnabled)
            {
                var fileLog = FileMessageTemplate.Format(now.ToString("yyyy/MM/dd HH:mm:ss"), status, context, message);
                var bytes = fileLog.GetBytes();

                if (Instance.FileDate < DateTime.Today)
                {
                    SetupFileLogging(true);
                }

                lock (Instance.FileWriteMutex)
                {
                    Instance.FileStream.Write(bytes, 0, bytes.Length);
                    Instance.FileLogCounter++;

                    if (Instance.FileLogCounter > Instance.FileLogFlushThreshold)
                    {
                        Instance.FileStream.Flush();
                        Instance.FileLogCounter = 0;
                    }
                }
            }
        }
    }
}
