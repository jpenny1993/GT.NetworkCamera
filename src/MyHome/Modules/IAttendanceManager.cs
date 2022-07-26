using System;

namespace MyHome.Modules
{
    public interface IAttendanceManager
    {
        event AttendanceManager.EventHandler OnAccessDenied;
        event AttendanceManager.ScanEventHandler OnScannedKeycard;

        void Initialise(IFileManager fm, bool allowNewUsers, TimeSpan openingHours, TimeSpan closingHours);

        void ClockIn(IFileManager fm, DateTime timestamp, string rfid);

        void ClockIn(IFileManager fm, DateTime timestamp, string rfid, string reason);

        void ClockOut(IFileManager fm, DateTime timestamp, string rfid);

        void ClockOut(IFileManager fm, DateTime timestamp, string rfid, string reason);

        void AutoClockOut(IFileManager fm);
    }
}
