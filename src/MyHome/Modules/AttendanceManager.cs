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

namespace MyHome.Modules
{
    public class AttendanceManager : IAttendanceManager
    {
        public delegate void EventHandler();
        public delegate void ScanEventHandler(string rfid, string displayName, string status);

        private const string UserAccountsFilePath = Directories.Config + "\\users.csv";

        private static string AttendanceFilePath { get { return Path.Combine(Directories.Config,  "attendance_{0}.csv".Format(DateTime.Today.Monthstamp())); } }

        private readonly Logger _logger;
        private readonly RFIDReader _rfid;
        private readonly Hashtable _users;

        private bool _allowNewUsers;
        private TimeSpan _openingHours;
        private TimeSpan _closingHours;

        public event AttendanceManager.EventHandler OnAccessDenied;

        public event AttendanceManager.ScanEventHandler OnScannedKeycard;

        public AttendanceManager(RFIDReader rfidReader)
        {
            _logger = Logger.ForContext(this);
            _allowNewUsers = false;
            _rfid = rfidReader;
            _rfid.IdReceived += Rfid_IdReceived;
            _rfid.MalformedIdReceived += Rfid_MalformedIdReceived;
            _users = new Hashtable();
        }

        public void Initialise(IFileManager fm, bool allowNewUsers, TimeSpan openingHours, TimeSpan closingHours)
        {
            _logger.Information("Initialising user accounts");
            _allowNewUsers = allowNewUsers;
            _openingHours = openingHours;
            _closingHours = closingHours;
            
            // Create users file if missing
            if (!fm.FileExists(UserAccountsFilePath))
            {
                fm.SaveFile(UserAccountsFilePath, string.Empty);
            }

            // Get users file
            var file = fm.GetFileString(UserAccountsFilePath);
            var rows = file.Split('\r', '\n');

            // For each line create user model
            foreach (var line in rows)
            {
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

        public void ClockIn(IFileManager fm, DateTime timestamp, string rfid)
        {
            ClockIn(fm, timestamp, rfid, string.Empty);
        }

        public void ClockIn(IFileManager fm, DateTime timestamp, string rfid, string reason)
        {
            if (!fm.HasFileSystem) return;

            var user = FindUser(rfid);
            if (user == null) return;

            _logger.Information("Performing user clock-in");
            using (var fs = fm.GetFileStream(AttendanceFilePath, FileMode.Append, FileAccess.Write))
            {
                AppendAttendance(fs, timestamp, rfid, AttendanceStatus.ClockIn, reason);
                fs.Flush();
                fs.Close();
            }

            user.LastClockedIn = timestamp;
            SaveUserAccountsToFile(fm);
        }

        public void ClockOut(IFileManager fm, DateTime timestamp, string rfid)
        {
            ClockOut(fm, timestamp, rfid, string.Empty);
        }

        public void ClockOut(IFileManager fm, DateTime timestamp, string rfid, string reason)
        {
            if (!fm.HasFileSystem) return;

            var user = FindUser(rfid);
            if (user == null) return;

            _logger.Information("Performing user clock-out");
            using (var fs = fm.GetFileStream(AttendanceFilePath, FileMode.Append, FileAccess.Write))
            {
                AppendAttendance(fs, timestamp, rfid, AttendanceStatus.ClockOut, reason);
                fs.Flush();
                fs.Close();
            }

            user.LastClockedOut = timestamp;
            SaveUserAccountsToFile(fm);
        }

        public void AutoClockOut(IFileManager fm)
        {
            if (DateTime.Now.TimeOfDay < _closingHours) return;

            if (!fm.HasFileSystem) return;

            _logger.Information("Running auto clock-out");
            using (var fs = fm.GetFileStream(AttendanceFilePath, FileMode.Append, FileAccess.Write))
            {
                foreach (UserAccount user in _users.Values)
                {
                    if (user.LastClockedIn > DateTime.Today &&      // has clocked-in today
                        user.LastClockedOut < user.LastClockedIn)   // has clocked-in again since last clock-out
                    {
                        var closingTime = DateTime.Today.Add(_closingHours);
                        AppendAttendance(fs, closingTime, user.RFID, AttendanceStatus.ClockOut, "Automated clock-out by system");
                        user.LastClockedOut = closingTime;
                    }
                }
                fs.Flush();
                fs.Close();
            }

            SaveUserAccountsToFile(fm);
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
                ? DateTimeParser.SortableDateTime(lastClockedIn, out lastClockIn)
                : false;

            DateTime lastClockOut = DateTime.MinValue;
            var clockOutSet = !StringExtensions.IsNullOrEmpty(lastClockedOut)
                ? DateTimeParser.SortableDateTime(lastClockedOut, out lastClockIn)
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

        private void AppendAttendance(FileStream fs, DateTime timestamp, string rfid, string status, string reason)
        {
            var bytes = "{0},{1},{2},{3}"
                    .Format(timestamp.SortableDateTime(), rfid, status, reason)
                    .GetBytes();
            fs.Write(bytes, 0, bytes.Length);
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
            foreach (UserAccount user in _users.Values)
            {
                builder.AppendLine(
                    "{0},{1},{2},{3}".Format(
                        user.RFID,
                        user.DisplayName,
                        user.LastClockedIn.SortableDateTime(),
                        user.LastClockedOut.SortableDateTime()
                    )
                );
            }

            fm.SaveFile(UserAccountsFilePath, builder.ToString());
        }

        private void Rfid_MalformedIdReceived(RFIDReader sender, EventArgs e)
        {
            _logger.Warning("Malformed RFID received...");
            if (OnAccessDenied != null)
            {
                OnAccessDenied.Invoke();
            }
        }

        private void Rfid_IdReceived(RFIDReader sender, string rfid)
        {
            _logger.Information("RFID received...");
            var user = FindUser(rfid);
            
            if (user == null && !_allowNewUsers)
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