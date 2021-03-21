using System;
using System.Collections;
using System.IO;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;

using Gadgeteer.Networking;
using GT = Gadgeteer;

using MyHome.Constants;
using MyHome.Extensions;
using MyHome.Models;
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


        private readonly ISystemManager _sys;
        private readonly ICameraManager _cam;
        private readonly IFileManager _fm;
        private readonly IWeatherManager _we;
        private bool _isRunning;

        public WebsiteManager(
            ISystemManager systemManager,
            ICameraManager cameraManager,
            IFileManager fileManager,
            IWeatherManager weatherManager)
        {
            _sys = systemManager;
            _cam = cameraManager;
            _fm = fileManager;
            _we = weatherManager;
        }

        private void RegisterWebEvents() 
        {
            Register(WebEvent_Index, WebServer.DefaultEvent);
            Register(WebEvent_CameraImage, WebRoutes.CameraImage);
            Register(WebEvent_CameraTimestamp, WebRoutes.CameraTimestamp);
            Register(WebEvent_GalleryCount, WebRoutes.GalleryCount);
            Register(WebEvent_GalleryList, WebRoutes.GalleryList);
            Register(WebEvent_GalleryImage, WebRoutes.GalleryImage);
            Register(WebEvent_WeatherLuminosity, WebRoutes.WeatherLuminosity);
            Register(WebEvent_WeatherHumidity, WebRoutes.WeatherHumidity);
            Register(WebEvent_WeatherTemperature, WebRoutes.WeatherTemperature);
            Register(WebEvent_SystemUptime, WebRoutes.SystemUptime);
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

        private WebsiteReponse GetFileResponse(string area, string path)
        {
            var response = new WebsiteReponse();
            if (!_fm.HasFileSystem()) return response;

            // Check area exists on the device
            if (!_fm.RootDirectoryExists(area)) return response;

            // Define the full filepath
            var filePath = MyPath.Combine(area, path);

            // Get directory from path
            var directory = Path.GetDirectoryName(filePath);

            // Check for file
            if (!_fm.FileExists(filePath)) return response;

            // Get file content
            response.ContentType = GetContentType(Path.GetExtension(filePath));
            response.Content = _fm.GetFileContent(filePath);
            response.Found = true;

            return response;
        }

        private WebsiteReponse GetJsonReponse(object response)
        {
            return new WebsiteReponse
            {
                Content = JsonConvert.SerializeObject(response).GetBytes(),
                ContentType = ContentTypes.Json,
                Found = response != null
            };
        }

        private WebsiteReponse BrowseDirectoryResponse(string area, string path, bool recursive)
        {
            var response = new WebsiteReponse();
            if (!_fm.HasFileSystem()) return response;

            // Check area exists on the device
            if (!_fm.RootDirectoryExists(area)) return response;

            // Define the full directory
            var systemPath = MyPath.Combine(area, path);

            if (!_fm.DirectoryExists(systemPath)) return response;

            string[] directories;
            string[] files;

            if (recursive)
            {
                directories = new string[0];
                files = _fm.ListFilesRecursive(systemPath).ToStringArray();
            }
            else
            {
                directories = _fm.ListDirectories(systemPath);
                files = _fm.ListFiles(systemPath);
            }

            var list = new ArrayList();
            foreach (var folder in directories)
            {
                var branch = PathObject.FromPath(area, folder, PathType.Directory, WebRoutes.GalleryList + "?directory=");
                list.Add(branch);
            }

            foreach (var file in files)
            {
                var leaf = PathObject.FromPath(area, file, PathType.File, WebRoutes.GalleryImage + '/');
                list.Add(leaf);
            }

            var json = JsonConvert.SerializeObject(list);
            Debug.Print(json);
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
            Debug.Print("WebsiteManager: Registering \"" + webRoute + "\"");
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
