using System;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;

using GT = Gadgeteer;
using MyHome.Configuration;

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

            // Check the hardware is ready first as it's a less expensive operation
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
            if (_configuration.TakePicturesFrom == _configuration.TakePicturesUntil) return;

            var isFromEarlyTillLate = _configuration.TakePicturesFrom < _configuration.TakePicturesUntil;

            // Allow morning till evening
            if (isFromEarlyTillLate && (timestamp.TimeOfDay < _configuration.TakePicturesFrom || timestamp.TimeOfDay > _configuration.TakePicturesUntil)) return;

            // Allow evening till afternoon
            if (!isFromEarlyTillLate && (timestamp.TimeOfDay < _configuration.TakePicturesFrom && timestamp.TimeOfDay > _configuration.TakePicturesUntil)) return;

            TakePicture();
        }

        /// <summary>
        /// Takes the picture, providing that the camera is ready.
        /// </summary>
        public void TakePicture()
        {
            if (_camera.CameraReady)
            {
                _logger.Information("Take picture");
                _camera.TakePicture();
            }
            else 
            {
                _logger.Information("Not ready to take picture");
            }
        }
    }
#pragma warning restore 0612, 0618
}
