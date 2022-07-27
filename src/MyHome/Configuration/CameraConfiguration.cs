using System;

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
    }
}
