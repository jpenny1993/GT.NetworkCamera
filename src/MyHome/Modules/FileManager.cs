using System;
using System.Collections;
using System.Text;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Gadgeteer.Modules.GHIElectronics;

using GT = Gadgeteer;
using MyHome.Extensions;

namespace MyHome.Modules
{
#pragma warning disable 0612, 0618 // Ignore MicroSDCard obsolete warning
    public sealed class FileManager
    {
        const string ImageDirectory = "DCIM";
        private readonly MicroSDCard _sdCard;

        public FileManager(MicroSDCard microSDCard)
        {
            _sdCard = microSDCard;
            _sdCard.Mounted += MicroSDCard_Mounted;
            _sdCard.Unmounted += MicroSDCard_Unmounted;
        }

        private bool HasFileSystem() 
        {
            return _sdCard.IsCardInserted && _sdCard.IsCardMounted;
        }

        private void MicroSDCard_Mounted(MicroSDCard sender, GT.StorageDevice device)
        {
            Debug.Print("SD Card: Mounted");

            Debug.Print("SD Card: Listing directories");
            var directories = sender.StorageDevice.ListRootDirectorySubdirectories();

            Debug.Print("SD Card: Checking for " + ImageDirectory);
            if (!directories.Contains(ImageDirectory))
            {
                Debug.Print("SD Card: Creating directory");
                sender.StorageDevice.CreateDirectory(ImageDirectory);
                Debug.Print("SD Card: Created directory");
            }
        }

        private void MicroSDCard_Unmounted(MicroSDCard sender, EventArgs e)
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
