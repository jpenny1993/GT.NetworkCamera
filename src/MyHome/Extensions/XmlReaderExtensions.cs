using MyHome.Models;
using System;
using System.Collections;
using System.Xml;

namespace MyHome.Extensions
{
    public static class XmlReaderExtensions
    {
        public static XmlElement ReadXmlElement(this XmlReader reader)
        {
            var element = new XmlElement();
            reader.Read();
            element.Name = reader.Name;
            reader.Read();
            element.Value = reader.Value;
            reader.Read();
            return element;
        }

        public static XmlElement Validate(this XmlElement element, string elementName)
        {
            if (element.Name != elementName)
            {
                throw new InvalidOperationException(
                    "Configuration file is corrupt, expected {0} element but found {1} element instead.".Format(elementName, element.Name));
            }

            return element;
        }

        public static bool GetBoolean(this XmlElement element)
        {
            return element.Value == "true" ? true : false;
        }

        public static int GetIntAbs(this XmlElement element)
        {
            return Math.Abs(int.Parse(element.Value));
        }

        public static DayOfWeek[] GetDayOfWeekArray(this XmlElement element)
        {
            var parts = element.Value.Split(',');
            var days = new DayOfWeek[parts.Length];

            for (var index = 0; index < parts.Length; index++)
            {
                switch (parts[index].Trim())
                {
                    default: break;
                    case "0":
                        days[index] = DayOfWeek.Sunday;
                        break;
                    case "1":
                        days[index] = DayOfWeek.Monday;
                        break;
                    case "2":
                        days[index] = DayOfWeek.Tuesday;
                        break;
                    case "3":
                        days[index] = DayOfWeek.Wednesday;
                        break;
                    case "4":
                        days[index] = DayOfWeek.Thursday;
                        break;
                    case "5":
                        days[index] = DayOfWeek.Friday;
                        break;
                    case "6":
                        days[index] = DayOfWeek.Saturday;
                        break;
                }
            }

            return days;
        }

        public static TimeSpan GetTimeSpan(this XmlElement element)
        {
            var parts = element.Value.Split(':');

            var hours = int.Parse(parts[0]);
            var minutes = int.Parse(parts[1]);
            var seconds = int.Parse(parts[2]);

            return new TimeSpan(hours, minutes, seconds);
        }
    }
}
