using System;
using System.Collections;
using System.Text;
using Gadgeteer.Modules.GHIElectronics;
using Microsoft.SPOT;
using MyHome.Constants;
using MyHome.Extensions;
using MyHome.Models;
using MyHome.Utilities;

using GT = Gadgeteer;
using System.IO;
using MyHome.Configuration;

namespace MyHome.Modules
{
    public class AttendanceManager : IAttendanceManager
    {
        public delegate void EventHandler();
        public delegate void ScanEventHandler(string rfid, string displayName, string status);

        private const string UsersCsvFilePath = Directories.Config + "\\users.csv";
        private const string UsersCsvRowTemplate = "{0},{1},{2},{3}\r\n";

        private static string AttendanceCsvFilePath { get { return Path.Combine(Directories.Attendance,  "attendance_log_{0}.csv".Format(DateTime.Today.Monthstamp())); } }
        private const string AttendanceCsvRowTemplate = "{0},{1},{2},{3}\r\n";

        private readonly Logger _logger;
        private readonly RFIDReader _rfid;
        private readonly IFileManager _fm;
        private readonly Hashtable _users;

        private AttendanceConfiguration _configuration;

        public event AttendanceManager.EventHandler OnAccessDenied;

        public event AttendanceManager.ScanEventHandler OnScannedKeycard;

        public AttendanceManager(RFIDReader rfidReader, IFileManager fm)
        {
            _logger = Logger.ForContext(this);
            _fm = fm;
            _rfid = rfidReader;
            _rfid.IdReceived += Rfid_IdReceived;
            _rfid.MalformedIdReceived += Rfid_MalformedIdReceived;
            _users = new Hashtable();
        }

        public void Initialise(AttendanceConfiguration configuration)
        {
            _configuration = configuration;

            if (!_fm.FileExists(UsersCsvFilePath)) return;

            // Get users file
            _logger.Information("Initialising user accounts");
            var file = _fm.GetFileString(UsersCsvFilePath);
            var rows = file.Split('\r', '\n');

            // For each line create user model
            for (int i = 1; i < rows.Length; i++)
            {
                var line = rows[i];
                var trimmedLine = line.Trim();
                if (trimmedLine.IsNullOrEmpty()) continue;

                var splitLine = trimmedLine.Split(',');
                if (splitLine.Length != 4)
                {
                    _logger.Warning("Ignoring unexpected user configuration \"{0}\"", splitLine);
                    continue;
                }

                AddUser(splitLine[0].Trim(), splitLine[1].Trim(), splitLine[2].Trim(), splitLine[3].Trim());
            }

            _logger.Information("Initialised all user accounts");
        }

        public void ClockIn(DateTime timestamp, string rfid)
        {
            ClockIn(timestamp, rfid, string.Empty);
        }

        public void ClockIn(DateTime timestamp, string rfid, string reason)
        {
            if (!_fm.HasFileSystem) return;

            var user = FindUser(rfid);
            if (user == null) return;

            _logger.Information("Performing user clock-in");
            var isFirstRow = !_fm.FileExists(AttendanceCsvFilePath);
            using (var fs = _fm.GetFileStream(AttendanceCsvFilePath, FileMode.Append, FileAccess.Write))
            {
                AppendAttendance(fs, timestamp, rfid, AttendanceStatus.ClockIn, reason, isFirstRow);
                fs.Flush();
                fs.Close();
            }

            user.LastClockedIn = timestamp;
            SaveUserAccountsToFile(_fm);
        }

        public void ClockOut(DateTime timestamp, string rfid)
        {
            ClockOut(timestamp, rfid, string.Empty);
        }

        public void ClockOut(DateTime timestamp, string rfid, string reason)
        {
            if (!_fm.HasFileSystem) return;

            var user = FindUser(rfid);
            if (user == null) return;

            _logger.Information("Performing user clock-out");
            var isFirstRow = !_fm.FileExists(AttendanceCsvFilePath);
            using (var fs = _fm.GetFileStream(AttendanceCsvFilePath, FileMode.Append, FileAccess.Write))
            {
                AppendAttendance(fs, timestamp, rfid, AttendanceStatus.ClockOut, reason, isFirstRow);
                fs.Flush();
                fs.Close();
            }

            user.LastClockedOut = timestamp;
            SaveUserAccountsToFile(_fm);
        }

        public void AutoClockOut(DateTime today, DateTime now)
        {
            if (now.TimeOfDay < _configuration.ClosingHours) return;

            if (!_fm.HasFileSystem) return;

            // if no file exists then people aren't clocked-in
            if (!_fm.FileExists(AttendanceCsvFilePath)) return;

            _logger.Information("Running auto clock-out");
            using (var fs = _fm.GetFileStream(AttendanceCsvFilePath, FileMode.Append, FileAccess.Write))
            {
                foreach (UserAccount user in _users.Values)
                {
                    if (user.LastClockedIn > today &&      // has clocked-in today
                        user.LastClockedOut < user.LastClockedIn)   // has clocked-in again since last clock-out
                    {
                        var closingTime = today.Add(_configuration.ClosingHours);
                        AppendAttendance(fs, closingTime, user.RFID, AttendanceStatus.ClockOut, "Automated clock-out by system", false);
                        user.LastClockedOut = closingTime;
                    }
                }
                fs.Flush();
                fs.Close();
            }

            SaveUserAccountsToFile(_fm);
        }

        /// <summary>
        /// Check working hours to identify overtime.
        /// </summary>
        public bool IsWithinWorkingHours(DateTime timestamp)
        {
            // Check day of week, .Contains() doesn't exist in .NetMF
            var isWorkingDay = false;
            foreach (var dayOfWeek in _configuration.WorkingDays)
            {
                isWorkingDay = dayOfWeek == timestamp.DayOfWeek;
                if (isWorkingDay) break;
            }
            if (!isWorkingDay) return false;

            var isDuringWorkingHours = timestamp.IsInRange(_configuration.OpeningHours, _configuration.ClosingHours);
            return isDuringWorkingHours;
        }

        public bool IsWithinOpeningGracePeriod(DateTime timestamp)
        {
            return _configuration.OpeningGracePeriod.IsInRange(timestamp);
        }

        public bool IsWithinClosingGracePeriod(DateTime timestamp)
        {
            return _configuration.ClosingGracePeriod.IsInRange(timestamp);
        }

        public bool HasUserClockedInOnDate(DateTime timestamp, string rfid)
        {
            var user = FindUser(rfid);

            // Handle sensible hours
            if (_configuration.OpeningHours < _configuration.ClosingHours)
            {
                return timestamp.Date == user.LastClockedIn.Date;
            }

            // Handle night shift
            if (timestamp.TimeOfDay > _configuration.OpeningHours)
            {
                return timestamp.Date == user.LastClockedIn.Date;
            }

            if (timestamp.TimeOfDay < _configuration.ClosingHours)
            {
                return timestamp.Date == user.LastClockedIn.Date.Subtract(new TimeSpan(1, 0 , 0, 0));
            }

            return false; 
        }

        private UserAccount AddUser(string rfid, string displayName = null, string lastClockedIn = null, string lastClockedOut = null)
        {
            if (StringExtensions.IsNullOrEmpty(rfid))
            {
                _logger.Warning("RFID code cannot be empty");
                return null;
            }

            if (StringExtensions.IsNullOrEmpty(displayName))
            {
                displayName = "User {0}".Format(_users.Count + 1);
            }

            DateTime lastClockIn = DateTime.MinValue;
            var clockInSet = !StringExtensions.IsNullOrEmpty(lastClockedIn)
                ? DateTimeParser.SortableDateTime(lastClockedIn, out lastClockIn) ||
                  DateTimeParser.ISO_UK(lastClockedIn, out lastClockIn)
                : false;

            DateTime lastClockOut = DateTime.MinValue;
            var clockOutSet = !StringExtensions.IsNullOrEmpty(lastClockedOut)
                ? DateTimeParser.SortableDateTime(lastClockedOut, out lastClockIn) ||
                  DateTimeParser.ISO_UK(lastClockedIn, out lastClockIn)
                : false;
            
            var user = new UserAccount
            {
                RFID = rfid,
                DisplayName = displayName,
                LastClockedIn = lastClockIn,
                LastClockedOut = lastClockOut
            };

            if (!_users.Contains(rfid))
            { 
                _users.Add(rfid, user);
            }

            return user;
        }

        private void AppendAttendance(FileStream fs, DateTime timestamp, string rfid, string status, string reason, bool isFirstRow)
        {
            if (isFirstRow)
            {
                fs.WriteText(AttendanceCsvRowTemplate, "Timestamp", "RFID", "Status", "Reason");
            }

            fs.WriteText(AttendanceCsvRowTemplate, timestamp.SortableDateTime(), rfid, status, reason);
        }

        private UserAccount FindUser(string rfid)
        {
            if (_users.Contains(rfid))
            {
                return (UserAccount)_users[rfid];
            }

            return null;
        }

        private void UpdateUser(UserAccount user)
        {
            if (_users.Contains(user.RFID))
            {
                _users[user.RFID] = user;
            }
        }

        private void SaveUserAccountsToFile(IFileManager fm)
        {
            var builder = new StringBuilder();

            builder.Append(UsersCsvRowTemplate.Format(
                "RFID",
                "Display name",
                "Last clock-in",
                "Last clock-out"
                )
            );

            foreach (UserAccount user in _users.Values)
            {
                builder.Append(
                    UsersCsvRowTemplate.Format(
                        user.RFID,
                        user.DisplayName,
                        user.LastClockedIn.SortableDateTime(),
                        user.LastClockedOut.SortableDateTime()
                    )
                );
            }

            fm.SaveFile(UsersCsvFilePath, builder.ToString());
        }

        private void Rfid_MalformedIdReceived(RFIDReader sender, EventArgs e)
        {
            if (_configuration == null)
            {
                _logger.Information("Malformed RFID received before configuration was set.");
                return;
            }

            _logger.Warning("Malformed RFID received...");
            if (OnAccessDenied != null)
            {
                OnAccessDenied.Invoke();
            }
        }

        private void Rfid_IdReceived(RFIDReader sender, string rfid)
        {
            if (_configuration == null) 
            {
                _logger.Information("RFID received before configuration was set.");
                return;
            }

            _logger.Information("RFID received...");
            var user = FindUser(rfid);
            
            if (user == null && !_configuration.AllowNewUsers)
            {
                if (OnAccessDenied != null)
                {
                    OnAccessDenied.Invoke();
                }

                return;
            }
            
            if (user == null)
            {
                user = AddUser(rfid);
            }

            if (OnScannedKeycard != null && user != null)
            {
                var clockInClockOut = user.LastClockedOut < user.LastClockedIn
                    ? AttendanceStatus.ClockOut
                    : AttendanceStatus.ClockIn;

                OnScannedKeycard.Invoke(user.RFID, user.DisplayName, clockInClockOut);
            }
        }
    }
}