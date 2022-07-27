using System;

namespace MyHome.Configuration
{
    public sealed class AttendanceConfiguration
    {
        /// <summary>
        /// Allows for unregistered users to clock-in, and automatically be added to the system.
        /// </summary>
        public bool AllowNewUsers;

        /// <summary>
        /// The days of the week that I'm working.
        /// </summary>
        public DayOfWeek[] WorkingDays;

        /// <summary>
        /// The time working hours begin from.
        /// </summary>
        public TimeSpan OpeningHours;

        /// <summary>
        /// The time working hours finish at, used for auto clock-out and overtime.
        /// </summary>
        public TimeSpan ClosingHours;
    }
}
