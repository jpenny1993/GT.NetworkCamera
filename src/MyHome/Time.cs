using System;
using Microsoft.SPOT;
using MyHome.Utilities;

namespace MyHome
{
    static class Time
    {
        public static bool Bst = false;
        public static object TimeMutex = new object();

        public static bool SyncInternetTime()
        {
            Debug.Print("Attempting to synchronise time...");
            DateTime time;
            bool syncSuccess = false;
            string[] tempTimeServerArray = { "time.nist.gov", "time-c.nist.gov", "0.uk.pool.ntp.org" };
            lock (TimeMutex)
            {
                foreach (string server in tempTimeServerArray) 
                {
                    Debug.Print("Using server: " + server);
                    if (GetTime(server, out time)) //this needs to be done before any other part of the system can run, since its dependant on the correct time
                    {
                        if (DateTime.Now != time)
                        {
                            Microsoft.SPOT.Hardware.Utility.SetLocalTime(time); //set the system time
                        }
                        syncSuccess = true;
                        break;
                    }
                    else
                    {
                        Debug.Print("Time synchronisation failed.");
                    }
                }
            }
            if (!syncSuccess)
                Debug.Print("Synchronisation to all time servers failed.");
            else
                Debug.Print("Time Synchronised - " + DateTime.Now.ToString("dd/MM/yy HH:mm:ss."));
            return syncSuccess;
        }

        /// <summary>
        /// gets time in utc and converts it from utc to gmt
        /// </summary>
        public static bool GetTime(string timeServer, out DateTime currentTime)
        {
            string[] serverSplit = timeServer.Split('.');
            bool ntpServer = false;
            bool gotTime;
            foreach (string s in serverSplit)
                if (s == "ntp")
                {
                    ntpServer = true;
                    break;
                }
            if (ntpServer)
                gotTime = GetNtpTime(timeServer, 13, out currentTime);
            else
                gotTime = GetNistTime(timeServer, 13, out currentTime);
            if (IsBst(currentTime))
            {
                Bst = true;
                currentTime = currentTime.AddHours(1);
            }
            return gotTime;
        }

        public static bool GetNistTime(string serverAddress, Int32 serverPort, out DateTime time)
        {
            time = new DateTime(1970, 1, 1);
            SocketClient client = new SocketClient();
            try
            {
                client.ConnectSocket(serverAddress, serverPort);
                string message;
                if (client.GetMessage(out message))
                {
                    if (message == string.Empty)
                        if (client.GetMessage(out message))
                            return false;
                    //parse timeserver message
                    //56620 13-11-24 15:05:12 00 0 0 120.3 UTC(NIST) *
                    string[] split = message.Split(' ');
                    string[] splitDate = split[1].Split('-');
                    string[] splitTime = split[2].Split(':');

                    time = new DateTime(int.Parse("20" + splitDate[0]), int.Parse(splitDate[1]), int.Parse(splitDate[2]),
                        int.Parse(splitTime[0]), int.Parse(splitTime[1]), int.Parse(splitTime[2]));
                    return true;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                client.CloseConnection();
            }
            return false;
        }

        public static bool GetNtpTime(string serverAddress, Int32 serverPort, out DateTime time)
        {
            time = new DateTime(1970, 1, 1);
            SocketClient client = new SocketClient();
            try
            {
                client.ConnectSocket(serverAddress, serverPort);
                string message;
                if (client.GetMessage(out message))
                {
                    if (message == string.Empty)
                        if (client.GetMessage(out message))
                            return false;
                    //parse timeserver message
                    //11 FEB 2014 17:11:32 CET
                    string[] split = message.Split(' ');
                    string[] splitTime = split[3].Split(':');
                    int month = ParseMonth(split[1]);
                    if (month == 0)
                        throw new Exception("Month Parsing of NTP Pool Failed");
                    time = new DateTime(int.Parse(split[2]), month, int.Parse(split[0]),
                        int.Parse(splitTime[0]), int.Parse(splitTime[1]), int.Parse(splitTime[2]));
                    return true;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                client.CloseConnection();
            }
            return false;
        }

        private static int ParseMonth(string month)
        {
            string abrev = month.Substring(0, 3);
            if (abrev.Equals("JAN"))
                return 1;
            if (abrev.Equals("FEB"))
                return 2;
            if (abrev.Equals("MAR"))
                return 3;
            if (abrev.Equals("APR"))
                return 4;
            if (abrev.Equals("MAY"))
                return 5;
            if (abrev.Equals("JUN"))
                return 6;
            if (abrev.Equals("JUL"))
                return 7;
            if (abrev.Equals("AUG"))
                return 8;
            if (abrev.Equals("SEP"))
                return 9;
            if (abrev.Equals("OCT"))
                return 10;
            if (abrev.Equals("NOV"))
                return 11;
            if (abrev.Equals("DEC"))
                return 12;
            return 0;
        }

        //Always changes on the last Sunday of March and last Sunday of October
        public static bool IsBst(DateTime date)
        {
            //December to February are out
            if (date.Month < 3 || date.Month > 10) { return false; }
            //April to September are in
            if (date.Month > 3 && date.Month < 10) { return true; }
            //The earliest possible day for last sunday is 25th
            //we are DST if our previous sunday was on or after the 18th.
            int previousSunday = date.Day - (int)date.DayOfWeek;
            if (date.Month == 3)
                return previousSunday >= 18 && date.Hour >= 1; //in march the hour increases 1am
            return previousSunday >= 18 && date.Hour >= 2; //in october the hour decreases at 2am
        }

        public static bool SwitchToOrFromBst(DateTime date, out DateTime newDate)
        {
            bool bst = IsBst(date);
            bool updated = false;
            if (Bst && !bst)
            {
                updated = true;
                Bst = false;
                date.AddHours(-1);
            }
            else if (!Bst && bst)
            {
                updated = true;
                Bst = true;
                date.AddHours(1);
            }
            newDate = date;
            return updated;
        }
    }
}
