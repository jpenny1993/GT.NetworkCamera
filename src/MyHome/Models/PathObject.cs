using System;
using MyHome.Extensions;
using MyHome.Utilities;

namespace MyHome.Models
{
    public enum PathType
    {
        Directory,
        File
    }

    public class PathObject
    {
        public string FullPath;
        public string Url;
        public string Area;
        public string Directory;
        public string Name;
        public string Type;

        public static PathObject FromPath(string area, string fullPath, PathType type, string prefix = null)
        {
            var firstBreak = fullPath.IndexOf('\\');
            var path = fullPath.Substring(firstBreak);
            return new PathObject
            {
                FullPath = fullPath,
                Url = prefix + fullPath.TrimStart(area).ReplaceAll('\\', '/').TrimStart('/'),
                Area = fullPath.Substring(0, firstBreak),
                Directory = Path.GetDirectoryName(path).TrimStart('\\'),
                Name = Path.GetFilenameWithExtension(path),
                Type = type == PathType.Directory
                    ? "Directory"
                    : "File"
            };
        }
    }
}
