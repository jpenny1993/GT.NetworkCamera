using System;
using System.Collections;
using Microsoft.SPOT;
using Json.Lite;

namespace Json.ConsoleTest
{
    public class Program
    {
        public static void Main()
        {
            Print(null);
            Print("Hello World");
            Print(TestObject());
        }

        private static void Print(object obj)
        {
            var str = JsonConvert.SerializeObject(obj);
            Debug.Print(str);
        }

        private static Thing1 TestObject()
        {
            var table = new Hashtable();
            table.Add(1, "One");
            table.Add(2, "Two");
            table.Add(3, "Three");

            var data = new Thing3();
            data.Wibble = "Wobble";

            var anonymous = new Thing1
            {
                Id = 1,
                Name = "Test 123",
                Enum = TestEnum.Three,
                Boolean = true,
                StartDate = new DateTime(2021, 03, 14, 13, 45, 43),
                Uptime = new TimeSpan(1, 30, 15),
                HashTable = table,
                Array = new double[] 
                {
                    1.1,
                    2.2,
                    3.3,
                    4.4,
                    5.5
                },
                MyObject = new Thing2
                {
                    File = "consolelog.txt",
                    MaxSizeMb = 50,
                },
                MyStruct = data,
                EmptyArray = new string[0],
                EmptyObject = new object(),
                NullObject = null
            };
            return anonymous;
        }
    }

    public enum TestEnum
    {
        Zero,
        One,
        Two, 
        Three,
    }

    public class Thing1
    {
        public int Id;
        public string Name;
        public TestEnum Enum;
        public bool Boolean;
        public DateTime StartDate;
        public TimeSpan Uptime;
        public Hashtable HashTable;
        public double[] Array;
        public Thing2 MyObject;
        public Thing3 MyStruct;
        public string[] EmptyArray;
        public object EmptyObject;
        public object NullObject;
    }

    public class Thing2
    {
        public string File;
        public int MaxSizeMb;
    }

    public struct Thing3
    {
        public string Wibble;
    }
}
