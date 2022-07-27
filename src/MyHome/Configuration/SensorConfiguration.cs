using System;

namespace MyHome.Configuration
{
    public class SensorConfiguration
    {
        /// <summary>
        /// Saves measurements to a rolling csv file on the SD card when <see langword="true" />.
        /// </summary>
        public bool SaveMeasurementsToSdCard;
    }
}
