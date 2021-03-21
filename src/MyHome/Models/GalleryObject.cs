using System;
using System.IO;
using MyHome.Extensions;
using MyHome.Utilities;

namespace MyHome.Models
{
    public class GalleryObject
    {
        public string Url;
        public DateTime DateTime;
        public string Name;

        public static GalleryObject FromPath(string area, string fullPath, string prefix)
        {
            var filename = Path.GetFileNameWithoutExtension(fullPath);
            return new GalleryObject
            {
                Url = prefix + fullPath.TrimStart(area).ReplaceAll('\\', '/').TrimStart('/'),
                DateTime = DateTimeParser.CustomFormat(filename, "IMG_", ".bmp"),
                Name = filename
            };
        }
    }
}
