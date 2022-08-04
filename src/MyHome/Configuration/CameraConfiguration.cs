using System;
using System.Ext.Xml;
using System.Text;
using System.Xml;
using Json.Lite;
using MyHome.Extensions;

namespace MyHome.Configuration
{
    public sealed class CameraConfiguration
    {
        /// <summary>
        /// Globally enable or disable automatic capturing of pictures.
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// Saves pictures to the SD card when <see langword="true" />.
        /// </summary>
        public bool SavePicturesToSdCard;

        /// <summary>
        /// Days to take pictures on. Currently theres no point at the weekend if the office curtains are closed.
        /// </summary>
        public DayOfWeek[] DaysToTakePicturesOn;

        /// <summary>
        /// The time to start taking pictures from. The camera doesn't have nightvision...
        /// </summary>
        public TimeSpan TakePicturesFrom;

        /// <summary>
        /// The time to stop taking pictures from. The camera doesn't have nightvision...
        /// </summary>
        public TimeSpan TakePicturesUntil;

        public static CameraConfiguration Read(XmlReader reader)
        {
            reader.Read(); // <Camera>

            var model = new CameraConfiguration
            {
                Enabled = reader.ReadXmlElement().Validate("Enabled").GetBoolean(),
                SavePicturesToSdCard = reader.ReadXmlElement().Validate("SavePicturesToSdCard").GetBoolean(),
                DaysToTakePicturesOn = reader.ReadXmlElement().Validate("DaysToTakePicturesOn").GetDayOfWeekArray(),
                TakePicturesFrom = reader.ReadXmlElement().Validate("TakePicturesFrom").GetTimeSpan(),
                TakePicturesUntil = reader.ReadXmlElement().Validate("TakePicturesUntil").GetTimeSpan()
            };

            reader.Read(); // </Camera>

            return model;
        }

        public static void Write(XmlWriter writer, CameraConfiguration camera)
        {
            writer.WriteStartElement("Camera");

            writer.WriteStartElement("Enabled");
            writer.WriteString(new StringBuilder().WriteBoolean(camera.Enabled).ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("SavePicturesToSdCard");
            writer.WriteString(new StringBuilder().WriteBoolean(camera.SavePicturesToSdCard).ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("DaysToTakePicturesOn");
            writer.WriteString(new StringBuilder().WriteArrayAsCsv(camera.DaysToTakePicturesOn).ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("TakePicturesFrom");
            writer.WriteString(new StringBuilder().WriteTimeSpan(camera.TakePicturesFrom).ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("TakePicturesUntil");
            writer.WriteString(new StringBuilder().WriteTimeSpan(camera.TakePicturesUntil).ToString());
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}
