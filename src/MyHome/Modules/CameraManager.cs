using System;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;
using MyHome.Configuration;
using MyHome.Constants;
using MyHome.Extensions;
using MyHome.Utilities;

using GT = Gadgeteer;

namespace MyHome.Modules
{
#pragma warning disable 0612, 0618 // Ignore Camera obsolete warning
    public sealed class CameraManager : MyHome.Modules.ICameraManager
    {
        private readonly Logger _logger;
        private readonly Camera _camera;
        private readonly ISystemManager _sys;
        private GT.Picture _picture;
        private DateTime _pictureLastTaken;

        private CameraConfiguration _configuration;
        private IAwaitable _savePictureThread = Awaitable.Default;

        public event CameraManager.PictureTakenEventHandler OnPictureTaken;

        public delegate void PictureTakenEventHandler(GT.Picture picture);

        public CameraManager(Camera camera, ISystemManager systemManager)
        {
            _logger = Logger.ForContext(this);
            _sys = systemManager;
            _camera = camera;
            _camera.CameraConnected += Camera_CameraConnected;
            _camera.CameraDisconnected += Camera_CameraDisconnected;
            _camera.PictureCaptured += Camera_PictureCaptured;
        }

        public void Initialise(CameraConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool Ready
        {
            get { return _camera.CameraReady; }
        }

        public GT.Picture Picture
        {
            get { return _picture; }
        }

        public DateTime PictureLastTaken
        {
            get { return _pictureLastTaken; }
        }

        private void Camera_CameraConnected(Camera sender, EventArgs e)
        {
            _logger.Information("Connected");
            sender.CurrentPictureResolution = Camera.PictureResolution.Resolution320x240;
            sender.TakePictureStreamTimeout = new TimeSpan(0, 0, 3);
        }

        private void Camera_CameraDisconnected(Camera sender, EventArgs e)
        {
            _logger.Information("Disconnected");
        }

        private void Camera_PictureCaptured(Camera sender, GT.Picture picture)
        {
            _logger.Information("Picture captured");
            _pictureLastTaken = _sys.Time;
            _picture = picture;
            if (OnPictureTaken != null) 
            {
                OnPictureTaken.Invoke(picture);
            }
        }

        /// <summary>
        /// Takes a picture provided that the camera is ready, and the timestamp is within the configured range.
        /// </summary>
        public void TakePicture(DateTime timestamp)
        {
            if (!_configuration.Enabled) return;
            if (_savePictureThread.IsRunning) return;
            if (!_camera.CameraReady) return;

            // Check day of week, .Contains() doesn't exist in .NetMF
            var canTakePicture = false;
            foreach (var dayOfWeek in _configuration.DaysToTakePicturesOn)
            {
                canTakePicture = dayOfWeek == timestamp.DayOfWeek;
                if (canTakePicture) break;
            }
            if (!canTakePicture) return;

            // Checking the current time is within the configured range
            if (timestamp.IsInRange(_configuration.TakePicturesFrom, _configuration.TakePicturesUntil))
            {
                TakePicture();
            }
        }

        /// <summary>
        /// Takes the picture, providing that the camera is ready.
        /// </summary>
        public void TakePicture()
        {
            if (_camera.CameraReady && !_savePictureThread.IsRunning)
            {
                _logger.Information("Take picture");
                _camera.TakePicture();
            }
            else 
            {
                _logger.Information("Not ready to take picture");
            }
        }

        public void SavePictureToSdCard(IFileManager fm, GT.Picture picture, DateTime timestamp)
        {
            if (!_configuration.SavePicturesToSdCard) return;
            if (!fm.HasFileSystem) return;

            var filename = string.Concat("IMG_", timestamp.Timestamp(), FileExtensions.Bitmap);
            var filepath = MyPath.Combine(Directories.Camera, timestamp.Datestamp(), filename);
            _savePictureThread = new Awaitable(() => fm.SaveFile(filepath, picture));
        }
    }
#pragma warning restore 0612, 0618
}
