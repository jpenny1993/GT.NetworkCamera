using System;
using System.Ext.Xml;
using System.Text;
using System.Xml;
using Json.Lite;
using MyHome.Extensions;

namespace MyHome.Configuration
{
    public sealed class NetworkConfiguration
    {
        /// <summary>
        /// Requests an IP address from the default gateway when <see langword="true" />.
        /// </summary>
        public bool UseDHCP;

        /// <summary>
        /// A static IP address to use if <see cref="UseDHCP" /> is <see langword="false" />.
        /// </summary>
        public string IPAddress;

        /// <summary>
        /// A static subnet mask to use if <see cref="UseDHCP" /> is <see langword="false" />.
        /// </summary>
        public string SubnetMask;

        /// <summary>
        /// A static default gateway to use if <see cref="UseDHCP" /> is <see langword="false" />.
        /// </summary>
        public string Gateway;

        public static NetworkConfiguration Read(XmlReader reader)
        {
            reader.Read(); // <Network>

            var model = new NetworkConfiguration
            {
                UseDHCP = reader.ReadXmlElement().Validate("UseDHCP").GetBoolean(),
                IPAddress = reader.ReadXmlElement().Validate("IPAddress").Value,
                SubnetMask = reader.ReadXmlElement().Validate("SubnetMask").Value,
                Gateway = reader.ReadXmlElement().Validate("Gateway").Value
            };

            reader.Read(); // </Network>

            return model;
        }

        public static void Write(XmlWriter writer, NetworkConfiguration network)
        {
            writer.WriteStartElement("Network");

            writer.WriteStartElement("UseDHCP");
            writer.WriteString(new StringBuilder().WriteBoolean(network.UseDHCP).ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("IPAddress");
            writer.WriteString(network.IPAddress);
            writer.WriteEndElement();

            writer.WriteStartElement("SubnetMask");
            writer.WriteString(network.SubnetMask);
            writer.WriteEndElement();

            writer.WriteStartElement("Gateway");
            writer.WriteString(network.Gateway);
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}
