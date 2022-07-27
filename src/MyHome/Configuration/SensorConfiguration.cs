using System;
using System.Ext.Xml;
using System.Text;
using System.Xml;
using Json.Lite;
using MyHome.Extensions;

namespace MyHome.Configuration
{
    public class SensorConfiguration
    {
        /// <summary>
        /// Saves measurements to a rolling csv file on the SD card when <see langword="true" />.
        /// </summary>
        public bool SaveMeasurementsToSdCard;

        public static SensorConfiguration Read(XmlReader reader)
        {
            reader.Read(); // <Sensors>

            var model = new SensorConfiguration
            {
                SaveMeasurementsToSdCard = reader.ReadXmlElement().Validate("SaveMeasurementsToSdCard").GetBoolean()
            };

            reader.Read(); // </Sensors>

            return model;
        }

        public static void Write(XmlWriter writer, SensorConfiguration sensors)
        {
            writer.WriteStartElement("Sensors");

            writer.WriteStartElement("SaveMeasurementsToSdCard");
            writer.WriteString(new StringBuilder().WriteBoolean(sensors.SaveMeasurementsToSdCard).ToString());
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}
