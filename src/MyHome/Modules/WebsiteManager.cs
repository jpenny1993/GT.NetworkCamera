using System;
using System.Collections;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;

using Gadgeteer.Networking;
using GT = Gadgeteer;

using MyHome.Constants;
using MyHome.Extensions;
using MyHome.Utilities;

namespace MyHome.Modules
{
#pragma warning disable 0612, 0618 // Ignore MicroSDCard obsolete warning
    public sealed class WebsiteManager
    {
        private class WebsiteReponse
        {
            public bool Found { get; set; }
            public byte[] Content { get; set; }
            public string ContentType { get; set; }
        }

        private readonly MicroSDCard _sdCard;
        private GT.Picture _picture;
        private bool _isRunning;

        public WebsiteManager(MicroSDCard microSDCard)
        {
            _sdCard = microSDCard;
        }

        private void RegisterWebEvents() 
        {
            Debug.Print("Registering Index Page");
            Register(WebEvent_Index, WebServer.DefaultEvent);

            Debug.Print("Registering Camera Image");
            Register(WebEvent_CameraImage, WebRoutes.Camera);

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

        private WebsiteReponse GetFile(string area, string path)
        {
            var response = new WebsiteReponse();
            if (!_sdCard.IsCardInserted || !_sdCard.IsCardMounted)
            {
                return response;
            }

            // Check area exists on the device
            var directories = _sdCard.StorageDevice.ListRootDirectorySubdirectories();
            if (!directories.Contains(area))
            {
                return response;
            }

            // Define the full filepath
            var filePath = Path.Combine(area, path);

            // Get directory from path
            var directory = Path.GetDirectoryName(filePath);

            // Check for file
            var files = _sdCard.StorageDevice.ListFiles(directory);
            if (!files.Contains(filePath))
            {
                return response;
            }

            // Get file content
            response.ContentType = GetContentType(Path.GetFileExtension(path));
            response.Content = _sdCard.StorageDevice.ReadFile(filePath);
            response.Found = true;

            return response;
        }

        private WebsiteReponse ListFiles(string area, string path)
        {
            var response = new WebsiteReponse();
            if (!_sdCard.IsCardInserted || !_sdCard.IsCardMounted)
            {
                return response;
            }

            // Check area exists on the device
            var rootDirectories = _sdCard.StorageDevice.ListRootDirectorySubdirectories();
            if (!rootDirectories.Contains(area))
            {
                return response;
            }

            // Define the full directory
            var folderPath = Path.Combine(area, path);

            // Check for files
            var directories = _sdCard.StorageDevice.ListDirectories(folderPath);
            var files = _sdCard.StorageDevice.ListFiles(folderPath);

            // TODO Convert to JSON
            response.Content = System.Text.Encoding.UTF8.GetBytes(string.Concat(directories, files));
            response.ContentType = ContentTypes.Json;
            response.Found = true;

            return response;
        }

        private void WebEvent_Index(string path, WebServer.HttpMethod method, Responder responder)
        {
            if (path.IsNullOrEmpty())
            {
                path = WebRoutes.Index;
            }

            var file = GetFile(Directories.Website, path);
            SendResponse(file, responder);
        }

        private void WebEvent_CameraImage(string path, WebServer.HttpMethod method, Responder responder)
        {
            // Handle the return of latest image
            if (_picture != null)
            {
                responder.Respond(_picture);
            }
            else
            {
                var notFound = GetFile(Directories.Config, "ImageNotAvailable.bmp");
                SendResponse(notFound, responder);
            }
        }

        private void WebEvent_GalleryImage(string path, WebServer.HttpMethod method, Responder responder)
        {
            // Handle the return of stored images
            var file = GetFile(Directories.Camera, path);
            SendResponse(file, responder);
        }

        private void WebEvent_GalleryList(string path, WebServer.HttpMethod method, Responder responder)
        {
            // Return a list of stored images
            var folderPath = responder.UrlParameters.Contains(QueryStrings.Directory)
                ? responder.UrlParameters[QueryStrings.Directory].ToString()
                : string.Empty;

            var response = ListFiles(Directories.Camera, folderPath);
            SendResponse(response, responder);
        }

        private static string GetContentType(string fileExtension)
        {
            switch (fileExtension)
            {
                default: return ContentTypes.Binary;
                case ".bmp": return ContentTypes.ImageBmp;
                case ".css": return ContentTypes.Stylesheet;
                case ".gif": return ContentTypes.ImageGif;
                case ".html": return ContentTypes.TextHtml;
                case ".jpg":
                case ".jpeg": return ContentTypes.ImageJpeg;
                case ".js": return ContentTypes.Javascript;
                case ".log":
                case ".txt": return ContentTypes.TextPlain;
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
