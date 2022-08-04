using System;
using System.Ext.Xml;
using System.Text;
using System.Xml;
using Json.Lite;
using MyHome.Extensions;

namespace MyHome.Configuration
{
    public sealed class AttendanceConfiguration
    {
        public sealed class GracePeriod
        {
            public GracePeriod(TimeSpan time, int gracePeriodInMins)
            {
                Early = time.Subtract(TimeSpan.FromTicks(TimeSpan.TicksPerMinute * gracePeriodInMins));
                Late = time.Add(TimeSpan.FromTicks(TimeSpan.TicksPerMinute * gracePeriodInMins));
            }

            public TimeSpan Early { get; private set; }

            public TimeSpan Late { get; private set; }

            public bool IsInRange(DateTime timestamp)
            {
                return timestamp.IsInRange(Early, Late);
            }
        }

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

        public int GracePeriodInMinutes;

        private GracePeriod _openingGracePeriod = null;
        public GracePeriod OpeningGracePeriod 
        { 
            get
            {
                if (_openingGracePeriod == null) 
                {
                    _openingGracePeriod = new GracePeriod(OpeningHours, GracePeriodInMinutes);
                }

                return _openingGracePeriod;
            } 
        }

        private GracePeriod _closingGracePeriod = null;
        public GracePeriod ClosingGracePeriod 
        {
            get 
            {
                if (_closingGracePeriod == null)
                {
                    _closingGracePeriod = new GracePeriod(ClosingHours, GracePeriodInMinutes);
                }

                return _closingGracePeriod;
            }
        }

        public static AttendanceConfiguration Read(XmlReader reader)
        {
            reader.Read(); // <Attendance>

            var model = new AttendanceConfiguration
            {
                AllowNewUsers = reader.ReadXmlElement().Validate("AllowNewUsers").GetBoolean(),
                WorkingDays = reader.ReadXmlElement().Validate("WorkingDays").GetDayOfWeekArray(),
                OpeningHours = reader.ReadXmlElement().Validate("OpeningHours").GetTimeSpan(),
                ClosingHours = reader.ReadXmlElement().Validate("ClosingHours").GetTimeSpan(),
                GracePeriodInMinutes = reader.ReadXmlElement().Validate("GracePeriodInMinutes").GetIntAbs(),
            };

            reader.Read(); // </Attendance>

            return model;
        }

        public static void Write(XmlWriter writer, AttendanceConfiguration attendance)
        {
            writer.WriteStartElement("Attendance");

            writer.WriteStartElement("AllowNewUsers");
            writer.WriteString(new StringBuilder().WriteBoolean(attendance.AllowNewUsers).ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("WorkingDays");
            writer.WriteString(new StringBuilder().WriteArrayAsCsv(attendance.WorkingDays).ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("OpeningHours");
            writer.WriteString(new StringBuilder().WriteTimeSpan(attendance.OpeningHours).ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("ClosingHours");
            writer.WriteString(new StringBuilder().WriteTimeSpan(attendance.ClosingHours).ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("GracePeriodInMinutes");
            writer.WriteString(attendance.GracePeriodInMinutes.ToString());
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}
