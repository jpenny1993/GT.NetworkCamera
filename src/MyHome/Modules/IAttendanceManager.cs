using System;

namespace MyHome.Modules
{
    public interface IAttendanceManager
    {
        event AttendanceManager.EventHandler OnAccessDenied;
        event AttendanceManager.ScanEventHandler OnScannedKeycard;

        void Initialise(bool allowNewUsers, TimeSpan openingHours, TimeSpan closingHours);

        void ClockIn(DateTime timestamp, string rfid);

        void ClockIn(DateTime timestamp, string rfid, string reason);

        void ClockOut(DateTime timestamp, string rfid);

        void ClockOut(DateTime timestamp, string rfid, string reason);

        void AutoClockOut(DateTime today, DateTime now);
    }
}
