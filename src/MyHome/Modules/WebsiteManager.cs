using System;
using System.Collections;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;

using Gadgeteer.Networking;
using GT = Gadgeteer;

using MyHome.Constants;
using MyHome.Extensions;
using MyHome.Utilities;
using Json.Lite;

namespace MyHome.Modules
{
#pragma warning disable 0612, 0618 // Ignore MicroSDCard obsolete warning
    public sealed partial class WebsiteManager
    {
        private class WebsiteReponse
        {
            public bool Found { get; set; }
            public byte[] Content { get; set; }
            public string ContentType { get; set; }
        }

        private readonly IFileManager _fm;
        private GT.Picture _picture;
        private bool _isRunning;

        public WebsiteManager(IFileManager fileManager)
        {
            _fm = fileManager;
        }

        private void RegisterWebEvents() 
        {
            Debug.Print("Registering Index Page");
            Register(WebEvent_Index, WebServer.DefaultEvent);

            Debug.Print("Registering Camera Image");
            Register(WebEvent_CameraImage, WebRoutes.Camera);

            Debug.Print("Registering Gallery Count");
            Register(WebEvent_GalleryCount, WebRoutes.GalleryCount);

            Debug.Print("Registering Gallery List");
            Register(WebEvent_GalleryList, WebRoutes.GalleryList);

            Debug.Print("Registering Gallery Image");
            Register(WebEvent_GalleryImage, WebRoutes.GalleryImage);
        }

        public void Start(string ipAddress, ushort port = 80)
        {
            if (_isRunning)
            {
                Debug.Print("The web server is already running");
                return;
            }

            _isRunning = true;
            Debug.Print("Starting web server");
            WebServer.StartLocalServer(ipAddress, port);

            RegisterWebEvents();

            Debug.Print("Completed web server startup");
        }

        public void Stop()
        {
            WebServer.StopLocalServer();
            _isRunning = false;
        }

        public void UpdatePicture(GT.Picture picture)
        {
            _picture = picture;
        }

        private WebsiteReponse GetFileResponse(string area, string path)
        {
            var response = new WebsiteReponse();
            if (!_fm.HasFileSystem()) return response;

            // Check area exists on the device
            if (!_fm.RootDirectoryExists(area)) return response;

            // Define the full filepath
            var filePath = Path.Combine(area, path);

            // Get directory from path
            var directory = Path.GetDirectoryName(filePath);

            // Check for file
            if (!_fm.FileExists(filePath)) return response;

            // Get file content
            response.ContentType = GetContentType(Path.GetFileExtension(path));
            response.Content = _fm.GetFileContent(filePath);
            response.Found = true;

            return response;
        }

        private WebsiteReponse BrowseDirectoryResponse(string area, string path)
        {
            var response = new WebsiteReponse();
            if (!_fm.HasFileSystem()) return response;

            // Check area exists on the device
            if (!_fm.RootDirectoryExists(area)) return response;

            // Define the full directory
            var folderPath = Path.Combine(area, path);

            // Check for files
            var hashtable = new Hashtable();

            var directories = _fm.ListDirectories(folderPath);
            hashtable.Add("Directories", directories);

            var files = _fm.ListFiles(folderPath);
            hashtable.Add("Files", files);

            var json = JsonConvert.SerializeObject(hashtable);
            response.Content = json.GetBytes();
            response.ContentType = ContentTypes.Json;
            response.Found = true;

            return response;
        }

        private WebsiteReponse ListFilesResponse(string area, string path)
        {
            var response = new WebsiteReponse();
            if (!_fm.HasFileSystem()) return response;

            // Check area exists on the device
            if (!_fm.RootDirectoryExists(area)) return response;

            // Define the full directory
            var folderPath = Path.Combine(area, path);

            // Check for files
            var files = _fm.ListFilesRecursive(folderPath);
            var json = JsonConvert.SerializeObject(files);
            response.Content = json.GetBytes();
            response.ContentType = ContentTypes.Json;
            response.Found = true;

            return response;
        }

        private static string GetContentType(string fileExtension)
        {
            switch (fileExtension)
            {
                default: return ContentTypes.Binary;
                case FileExtensions.Bitmap:     return ContentTypes.Bitmap;
                case FileExtensions.Stylesheet: return ContentTypes.Stylesheet;
                case FileExtensions.Gif:        return ContentTypes.Gif;
                case FileExtensions.Html:       return ContentTypes.Html;
                case FileExtensions.Icon:       return ContentTypes.Icon;
                case FileExtensions.Jpg:
                case FileExtensions.Jpeg:       return ContentTypes.Jpeg;
                case FileExtensions.Javascript: return ContentTypes.Javascript;
                case FileExtensions.Log:        return ContentTypes.Text;
                case FileExtensions.Png:        return ContentTypes.Png;
                case FileExtensions.Text:       return ContentTypes.Text;
            }
        }

        private static void Register(WebEvent.ReceivedWebEventHandler handler, string webRoute)
        {
            Register(handler, WebServer.SetupWebEvent(webRoute));
        }

        private static void Register(WebEvent.ReceivedWebEventHandler handler, WebEvent webEvent)
        {
            webEvent.WebEventReceived += handler;
        }

        private static void SendResponse(WebsiteReponse response, Responder responder)
        {
            if (response.Found)
            {
                responder.Respond(response.Content, response.ContentType);
            }
            else
            {
                responder.NotFound();
            }
        }
    }
#pragma warning restore 0612, 0618
}
