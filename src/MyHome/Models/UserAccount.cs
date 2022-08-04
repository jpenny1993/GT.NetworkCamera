using System;
using Microsoft.SPOT;

namespace MyHome.Models
{
    public class UserAccount
    {
        public string RFID { get; set; }

        public string DisplayName { get; set; }

        public DateTime LastClockedIn { get; set; }

        public DateTime LastClockedOut { get; set; }
    }
}
