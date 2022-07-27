using System;

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
    }
}
