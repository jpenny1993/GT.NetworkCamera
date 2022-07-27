using System;

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
    }
}
