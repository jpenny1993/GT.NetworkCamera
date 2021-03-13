using System;

namespace MyHome.Modules
{
    public interface IFileManager
    {
        int CountFiles(string folderPath);
        bool DirectoryExists(string folderPath);
        bool FileExists(string filePath);
        byte[] GetFileContent(string filePath);
        bool HasFileSystem();
        string[] ListDirectories(string directory);
        string[] ListFiles(string directory);
        string[] ListRootDirectories();
        string[] ListRootFiles();
        bool RootDirectoryExists(string rootDirectory);
        void SaveFile(string filepath, Gadgeteer.Picture picture);
        void SaveFile(string filepath, Microsoft.SPOT.Bitmap bitmap);
        void SaveFile(string filepath, string text);
    }
}
