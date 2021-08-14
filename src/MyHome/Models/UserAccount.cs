using System;
using Microsoft.SPOT;

namespace MyHome.Models
{
    public class UserAccount
    {
        public string RFID { get; set; }

        public string Username { get; set; }

        public DateTime Allocated { get; set; }

        public DateTime Expired { get; set; }

        public bool IsExpired { get { return Expired != DateTime.MinValue; } }
    }
}
