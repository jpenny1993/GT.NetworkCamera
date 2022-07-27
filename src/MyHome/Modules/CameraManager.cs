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
