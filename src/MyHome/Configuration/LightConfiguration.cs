using System;
using System.Ext.Xml;
using System.Text;
using System.Xml;
using Json.Lite;
using MyHome.Extensions;

namespace MyHome.Configuration
{
    public sealed class LightConfiguration
    {
        /// <summary>
        /// The time to enable LEDs from.
        /// </summary>
        public TimeSpan LEDsOnFrom;

        /// <summary>
        /// The time to disable LEDs from, don't want to be a christmas tree when there's no-one around.
        /// </summary>
        public TimeSpan LEDsOnUntil;

        public static LightConfiguration Read(XmlReader reader)
        {
            reader.Read(); // <Lights>

            var model = new LightConfiguration
            {
                LEDsOnFrom = reader.ReadXmlElement().Validate("LEDsOnFrom").GetTimeSpan(),
                LEDsOnUntil = reader.ReadXmlElement().Validate("LEDsOnUntil").GetTimeSpan()
            };

            reader.Read(); // </Lights>

            return model;
        }

        public static void Write(XmlWriter writer, LightConfiguration lights)
        {
            writer.WriteStartElement("Lights");

            writer.WriteStartElement("LEDsOnFrom");
            writer.WriteString(new StringBuilder().WriteTimeSpan(lights.LEDsOnFrom).ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("LEDsOnUntil");
            writer.WriteString(new StringBuilder().WriteTimeSpan(lights.LEDsOnUntil).ToString());
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}
