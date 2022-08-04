using System;
using System.Collections;
using System.IO;

namespace MyHome.Modules
{
    public interface IFileManager
    {
        event FileManager.DeviceSwapEnventHandler OnDeviceSwap;

        bool HasFileSystem { get; }
        double TotalSizeInMb { get; }
        double TotalFreeSpaceInMb { get; }
        double TotalUsedSpaceInMb { get; }

        int CountFiles(string folderPath, bool recursive);
        void CreateDirectory(string folderPath);
        void DeleteDirectory(string folderPath);
        void DeleteFile(string filePath);
        bool DirectoryExists(string folderPath);
        bool FileExists(string filePath);
        byte[] GetFileContent(string filePath);
        string GetFileString(string filePath);
        FileStream GetFileStream(string filePath, FileMode mode, FileAccess access);
        string[] ListDirectories(string directory);
        ArrayList ListDirectoriesRecursive(string directory);
        string[] ListFiles(string directory);
        ArrayList ListFilesRecursive(string directory);
        string[] ListRootDirectories();
        string[] ListRootFiles();
        bool RootDirectoryExists(string rootDirectory);
        void SaveFile(string filepath, Gadgeteer.Picture picture);
        void SaveFile(string filepath, Microsoft.SPOT.Bitmap bitmap);
        void SaveFile(string filepath, string text);
    }
}
