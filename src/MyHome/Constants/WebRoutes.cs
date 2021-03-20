using System;
using Microsoft.SPOT;

namespace MyHome.Constants
{
    public static class WebRoutes
    {
        public const string Index = "index.html";

        public const string CameraImage = "api/camera/image";

        public const string CameraTimestamp = "api/camera/timestamp";

        public const string GalleryCount = "api/images/count";

        public const string GalleryList = "api/images/list";

        public const string GalleryImage = "api/images/view";

        public const string SystemUptime = "api/system/uptime";
    }
}
