using System;
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
        public string Area;
        public string Directory;
        public string Name;
        public PathType Type;

        public static PathObject FromDirectory(string fullPath)
        {
            return FromPathInteral(fullPath, PathType.Directory);
        }

        public static PathObject FromFile(string fullPath)
        {
            return FromPathInteral(fullPath, PathType.File);
        }

        public static PathObject FromPath(string fullPath)
        {
            return Path.GetFileExtension(fullPath).Length == 0
                ? FromDirectory(fullPath)
                : FromFile(fullPath);
        }

        private static PathObject FromPathInteral(string fullPath, PathType type)
        {
            var firstBreak = fullPath.IndexOf('\\');
            var area = fullPath.Substring(0, firstBreak);
            var path = fullPath.Substring(firstBreak);
            return new PathObject
            {
                FullPath = fullPath,
                Area = area,
                Directory = Path.GetDirectoryName(path),
                Name = Path.GetFilenameWithExtension(path),
                Type = type
            };
        }
    }
}
