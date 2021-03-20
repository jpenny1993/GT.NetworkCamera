using System;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;

using GT = Gadgeteer;

namespace MyHome.Modules
{
#pragma warning disable 0612, 0618 // Ignore Camera obsolete warning
    public sealed class CameraManager : MyHome.Modules.ICameraManager
    {
        private readonly Camera _camera;
        private readonly ISystemManager _sys;
        private GT.Picture _picture;
        private DateTime _pictureLastTaken;

        public event CameraManager.PictureTakenEventHandler OnPictureTaken;

        public delegate void PictureTakenEventHandler(GT.Picture picture);

        public CameraManager(Camera camera, ISystemManager systemManager)
        {
            _sys = systemManager;
            _camera = camera;
            _camera.CameraConnected += Camera_CameraConnected;
            _camera.CameraDisconnected += Camera_CameraDisconnected;
            _camera.PictureCaptured += Camera_PictureCaptured;
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
            Debug.Print("Camera: Connected");
            sender.CurrentPictureResolution = Camera.PictureResolution.Resolution320x240;
            sender.TakePictureStreamTimeout = new TimeSpan(0, 0, 3);
        }

        private void Camera_CameraDisconnected(Camera sender, EventArgs e)
        {
            Debug.Print("Camera: Disconnected");
        }

        private void Camera_PictureCaptured(Camera sender, GT.Picture picture)
        {
            Debug.Print("Camera: Picture captured");
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
                Debug.Print("Camera: Take picture");
                _camera.TakePicture();
            }
            else 
            {
                Debug.Print("Camera: Not ready to take picture");
            }
        }
    }
#pragma warning restore 0612, 0618
}
