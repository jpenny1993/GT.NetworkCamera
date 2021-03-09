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
            WebServer.DefaultEvent.WebEventReceived += WebEvent_Index;

            Debug.Print("Registering Image Page");
            var webEvents = WebServer.SetupWebEvent("image");
            webEvents.WebEventReceived += WebEvent_Image;
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

        private void WebEvent_Index(string path, WebServer.HttpMethod method, Responder responder)
        {
            /* // x.x.x.x:80/route?query=value
            if (responder.UrlParameters.Contains("query") &&
                responder.UrlParameters["query"].ToString() == "value")
            { 
            }
            */

            // Handle the return of existing images
            if (path.StartsWith(WebRoutes.Images))
            {
                var filepath = path.Remove(WebRoutes.Images);
                var file = GetFile(Directories.Camera, filepath);

                if (file.Found)
                    responder.Respond(file.Content, file.ContentType);
                else
                    responder.Respond("Not Found");
            }
            else if (!path.IsNullOrEmpty())
            {
                // TODO: handle addition of .html if required
                var file = GetFile(Directories.Website, path);

                if (file.Found)
                    responder.Respond(file.Content, file.ContentType);
                else
                    responder.Respond("Not Found");
            }
            else
            {
                // TODO: return index page
                responder.Respond("Hello World");
            }
        }

        private void WebEvent_Image(string path, WebServer.HttpMethod method, Responder responder)
        {
            if (_picture != null)
                responder.Respond(_picture);
            else
                responder.Respond("Latest image is not yet available.");
        }

        private WebsiteReponse GetFile(string area, string path)
        {
            var response = new WebsiteReponse();
            if (!_sdCard.IsCardInserted || !_sdCard.IsCardMounted)
            {
                return response;
            }

            // Check area exists on the device
            var directories =_sdCard.StorageDevice.ListRootDirectorySubdirectories();
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
            var fileExtension = Path.GetFileExtension(path);
            switch (fileExtension)
            {
                default:
                    response.ContentType = ContentTypes.Binary;
                    break;
                case ".bmp":
                    response.ContentType = ContentTypes.ImageBmp;
                    break;
                case ".gif":
                    response.ContentType = ContentTypes.ImageGif;
                    break;
                case ".html":
                    response.ContentType = ContentTypes.TextHtml;
                    break;
                case ".jpg":
                case ".jpeg":
                    response.ContentType = ContentTypes.ImageJpeg;
                    break;
                case ".log":
                case ".txt":
                    response.ContentType = ContentTypes.TextPlain;
                    break;
            }

            response.Content = _sdCard.StorageDevice.ReadFile(filePath);
            response.Found = true;
            return response;
        }
    }
#pragma warning restore 0612, 0618
}
