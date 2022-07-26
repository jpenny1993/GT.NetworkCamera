using System;

namespace MyHome.Models
{
    public class Attendance
    {
        public DateTime Timestamp { get; set; }

        public string UserId { get; set; }

        public string Status { get; set; }

        public string Reason { get; set; }
    }
}
