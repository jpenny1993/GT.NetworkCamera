using System;
namespace MyHome.Modules
{
    public interface ICameraManager
    {
        event CameraManager.PictureTakenEventHandler OnPictureTaken;
        Gadgeteer.Picture Picture { get; }
        DateTime PictureLastTaken { get; }
        bool Ready { get; }
        void TakePicture();
    }
}
