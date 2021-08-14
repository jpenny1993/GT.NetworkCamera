using System;
using System.IO;
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
#pragma warning disable 0612, 0618 // Ignore SDCard obsolete warning
    public sealed class FileManager : IFileManager
    {
        private readonly Logger _logger;
        private readonly SDCard _sdCard;

        public event FileManager.DeviceSwapEnventHandler OnDeviceSwap;

        public delegate void DeviceSwapEnventHandler(bool diskInserted);

        public FileManager(SDCard sdCard)
        {
            _logger = Logger.ForContext(this);
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
                _logger.Information("Creating directory \"{0}\"", folderPath);
                _sdCard.StorageDevice.CreateDirectory(folderPath);
            }
        }

        public bool DirectoryExists(string folderPath)
        {
            _logger.Information("Checking directory exists \"{0}\"", folderPath);
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

            _logger.Information("Getting file content \"{0}\"", filePath);
            return _sdCard.StorageDevice.ReadFile(filePath);
        }

        public FileStream GetFileStream(string filePath, FileMode mode, FileAccess access)
        {
            if (!HasFileSystem())
            {
                throw new ApplicationException("SD card not available to read");
            }

            _logger.Information("Getting file stream \"{0}\"", filePath);
            return _sdCard.StorageDevice.Open(filePath, mode, access);   
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
                    _logger.Information("Listing directories in \"{0}\"", directory);
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
                    _logger.Information("Listing files in \"{0}\"", directory);
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
                    _logger.Information("Listing root directories");
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
                    _logger.Information("Listing root files");
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
            _logger.Information("Checking directory exists \"{0}\"", rootDirectory);
            var directories = ListRootDirectories();
            return directories.ContainsCaseInsensitive(rootDirectory);
        }

        private void SDCard_Mounted(SDCard sender, GT.StorageDevice device)
        {
            _logger.Information("Mounted SD Card");

            _logger.Information("Listing directories");
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
                _logger.Information("Checking for \"{0}\"", dir);
                if (!directories.Contains(dir))
                {
                    _logger.Information("Creating directory");
                    sender.StorageDevice.CreateDirectory(dir);
                    _logger.Information("Created directory");
                }
            }

            if (OnDeviceSwap != null)
            {
                OnDeviceSwap.Invoke(true);
            }
        }

        private void SDCard_Unmounted(SDCard sender, EventArgs e)
        {
            _logger.Information("Unmounted SD Card");

            if (OnDeviceSwap != null)
            {
                OnDeviceSwap.Invoke(false);
            }
        }

        public void Remount() 
        {
            // Force card to remount on startup
            // Required to ensure directories are setup
            if (_sdCard.IsCardInserted)
            {
                if (_sdCard.IsCardMounted)
                {
                    _logger.Information("Remounting SD Card");
                    _sdCard.Unmount();

                    while (_sdCard.IsCardMounted)
                    {
                        Thread.Sleep(100);
                    }

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
            _logger.Information("Converting image to file format");
            var bytes = bitmap.GetBytes();
            _logger.Information("Saving file");
            _sdCard.StorageDevice.WriteFile(filepath, bytes);
            _logger.Information("File saved");
        }

        public void SaveFile(string filepath, string text)
        {
            if (HasFileSystem())
            {
                CreateDirectory(Path.GetDirectoryName(filepath));
                _logger.Information("Converting text to bytes");
                var bytes = Encoding.UTF8.GetBytes(text);
                _logger.Information("Saving file");
                _sdCard.StorageDevice.WriteFile(filepath, bytes);
                _logger.Information("File saved");
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
                _logger.Information("Converting picture to bitmap");
                var bitmap = picture.MakeBitmap();
                SaveBitmap(filepath, bitmap);
            }
        }

        public void WriteToFileStream(FileStream fs, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            fs.Write(text, 0, text.Length);
        }
    }
#pragma warning restore 0612, 0618
}
