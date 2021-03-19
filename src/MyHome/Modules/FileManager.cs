using System;
using System.Collections;
using System.Text;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Gadgeteer.Modules.GHIElectronics;

using GT = Gadgeteer;
using MyHome.Constants;
using MyHome.Extensions;
using MyHome.Utilities;

namespace MyHome.Modules
{
#pragma warning disable 0612, 0618 // Ignore MicroSDCard obsolete warning
    public sealed class FileManager : IFileManager
    {
        private readonly SDCard _sdCard;

        public FileManager(SDCard sdCard)
        {
            _sdCard = sdCard;
            _sdCard.Mounted += SDCard_Mounted;
            _sdCard.Unmounted += SDCard_Unmounted;
        }

        public int CountFiles(string folderPath, bool recursive)
        {
            var fileCount = ListFiles(folderPath).Length;
            if (recursive)
            {
                var directories = ListDirectories(folderPath);
                foreach (var directory in directories)
                {
                    fileCount += CountFiles(directory, recursive);
                }
            }

            return fileCount;
        }

        public void CreateDirectory(string folderPath)
        {
            if (!DirectoryExists(folderPath))
            {
                Debug.Print("SD Card: Creating directory \"" + folderPath + "\"");
                _sdCard.StorageDevice.CreateDirectory(folderPath);
            }
        }

        public bool DirectoryExists(string folderPath)
        {
            Debug.Print("SD Card: Checking directory exists \"" + folderPath + "\"");
            var directory = Path.GetDirectoryName(folderPath);
            var directories = ListDirectories(directory);
            return directories.ContainsCaseInsensitive(folderPath);
        }

        public bool FileExists(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            var files = ListFiles(directory);
            return files.ContainsCaseInsensitive(filePath);
        }

        public byte[] GetFileContent(string filePath)
        {
            if (!HasFileSystem())
            {
                throw new ApplicationException("SD card not available to read");
            }

            Debug.Print("SD Card: Getting file content \"" + filePath + "\"");
            return _sdCard.StorageDevice.ReadFile(filePath);
        }

        public bool HasFileSystem()
        {
            return _sdCard.IsCardInserted && _sdCard.IsCardMounted;
        }

        public string[] ListDirectories(string directory)
        {
            if (HasFileSystem())
            {
                try
                {
                    Debug.Print("SD Card: Listing directories in \"" + directory + "\"");
                    return _sdCard.StorageDevice.ListDirectories(directory);
                }
                catch
                {
                    // directory does not exist
                }
            }

            return new string[0];
        }

        public ArrayList ListDirectoriesRecursive(string directory)
        {
            if (HasFileSystem())
            {
                return GetDirectoriesRecursiveInternal(directory);
            }

            return new ArrayList();
        }

        public string[] ListFiles(string directory)
        {
            if (HasFileSystem())
            {
                try
                {
                    Debug.Print("SD Card: Listing files in \"" + directory + "\"");
                    return _sdCard.StorageDevice.ListFiles(directory);
                }
                catch
                {
                    // directory does not exist
                }
            }

            return new string[0];
        }

        public ArrayList ListFilesRecursive(string directory)
        {
            if (HasFileSystem())
            {
                return GetFilesRecursiveInternal(directory);
            }

            return new ArrayList();
        }

        private ArrayList GetDirectoriesRecursiveInternal(string directory)
        {
            ArrayList list = new ArrayList();
            var folders = ListDirectories(directory);
            foreach (var folder in folders)
            {
                list.Add(folder);

                var subDirectories = GetDirectoriesRecursiveInternal(folder);
                foreach (string entry in subDirectories)
                {
                    list.Add(entry);
                }
            }

            return list;
        }

        private ArrayList GetFilesRecursiveInternal(string directory)
        {
            ArrayList list = new ArrayList();
            var folders = ListDirectories(directory);
            foreach (var folder in folders)
            {
                var files = ListFiles(folder);
                foreach(var file in files)
                {
                    list.Add(file);
                }

                var subDirectories = ListDirectories(folder);
                foreach (var subFolder in subDirectories)
                {
                    var subTable = GetFilesRecursiveInternal(subFolder);
                    foreach (string entry in subTable)
                    {
                        list.Add(entry);
                    }
                }
            }

            return list;
        }

        public string[] ListRootDirectories()
        {
            if (HasFileSystem())
            {
                try
                {
                    Debug.Print("SD Card: Listing root directories");
                    return _sdCard.StorageDevice.ListRootDirectorySubdirectories();
                }
                catch
                {
                    // SD Card disconnected while reading
                }
            }

            return new string[0];
        }

        public string[] ListRootFiles()
        {
            if (HasFileSystem())
            {
                try
                {
                    Debug.Print("SD Card: Listing root files");
                    return _sdCard.StorageDevice.ListRootDirectoryFiles();
                }
                catch
                {
                    // SD Card disconnected while reading
                }
            }

            return new string[0];
        }

        public bool RootDirectoryExists(string rootDirectory)
        {
            Debug.Print("SD Card: Checking directory exists \"" + rootDirectory + "\"");
            var directories = ListRootDirectories();
            return directories.ContainsCaseInsensitive(rootDirectory);
        }

        private void SDCard_Mounted(SDCard sender, GT.StorageDevice device)
        {
            Debug.Print("SD Card: Mounted");

            Debug.Print("SD Card: Listing directories");
            var directories = sender.StorageDevice.ListRootDirectorySubdirectories();
            var expectedDirectories = new[] 
            {
                Directories.Camera,
                Directories.Config,
                Directories.Logs,
                Directories.Website
            };

            foreach (var dir in expectedDirectories)
            {
                Debug.Print("SD Card: Checking for " + dir);
                if (!directories.Contains(dir))
                {
                    Debug.Print("SD Card: Creating directory");
                    sender.StorageDevice.CreateDirectory(dir);
                    Debug.Print("SD Card: Created directory");
                }
            }
        }

        private void SDCard_Unmounted(SDCard sender, EventArgs e)
        {
            Debug.Print("SD Card: Unmounted");
        }

        public void Remount() 
        {
            // Force card to remount on startup
            // Required to ensure directories are setup
            if (_sdCard.IsCardInserted)
            {
                if (_sdCard.IsCardMounted)
                {
                    Debug.Print("SD Card: Remounting SD Card");
                    _sdCard.Unmount();

                    while (_sdCard.IsCardMounted)
                        Thread.Sleep(100);

                    _sdCard.Mount();
                }
                else
                {
                    _sdCard.Mount();
                }
            }
        }

        private void SaveBitmap(string filepath, Bitmap bitmap)
        {
            CreateDirectory(Path.GetDirectoryName(filepath));
            Debug.Print("SD Card: Converting image to file format");
            var bytes = bitmap.GetBytes();
            Debug.Print("SD Card: Saving file");
            _sdCard.StorageDevice.WriteFile(filepath, bytes);
            Debug.Print("SD Card: File saved");
        }

        public void SaveFile(string filepath, string text)
        {
            if (HasFileSystem())
            {
                CreateDirectory(Path.GetDirectoryName(filepath));
                Debug.Print("SD Card: Converting text to bytes");
                var bytes = Encoding.UTF8.GetBytes(text);
                Debug.Print("SD Card: Saving file");
                _sdCard.StorageDevice.WriteFile(filepath, bytes);
                Debug.Print("SD Card: File saved");
            }
        }

        public void SaveFile(string filepath, Bitmap bitmap) 
        {
            if (HasFileSystem())
            {
                SaveBitmap(filepath, bitmap);
            }
        }

        public void SaveFile(string filepath, GT.Picture picture)
        {
            if (HasFileSystem())
            {
                Debug.Print("SD Card: Converting picture to bitmap");
                var bitmap = picture.MakeBitmap();
                SaveBitmap(filepath, bitmap);
            }
        }
    }
#pragma warning restore 0612, 0618
}
