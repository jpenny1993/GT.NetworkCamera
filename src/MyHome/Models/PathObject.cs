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

        public static PathObject FromPath(string fullPath)
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
                Type = Path.GetFileExtension(path).Length > 0 
                    ? PathType.File 
                    : PathType.Directory
            };
        }
    }
}
