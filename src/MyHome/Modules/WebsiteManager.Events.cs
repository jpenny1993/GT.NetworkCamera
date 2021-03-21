using System;
using System.IO;
using System.Collections;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;

using Gadgeteer.Networking;
using GT = Gadgeteer;

using Json.Lite;
using MyHome.Constants;
using MyHome.Extensions;
using MyHome.Models;
using MyHome.Utilities;

namespace MyHome.Modules
{
    public sealed partial class WebsiteManager
    {
        private const string ImageNotAvailable = "ImageNotAvailable.bmp";

        /// <summary>
        /// Returns files from the wwwroot directory
        /// </summary>
        private void WebEvent_Index(string path, WebServer.HttpMethod method, Responder responder)
        {
            if (path.IsNullOrEmpty())
            {
                path = WebRoutes.Index;
            }

            var file = GetFileResponse(Directories.Website, path);
            SendResponse(file, responder);
        }

        /// <summary>
        /// Returns the latest image taken by the camera
        /// </summary>
        private void WebEvent_CameraImage(string path, WebServer.HttpMethod method, Responder responder)
        {
            if (_cam.Picture != null)
            {
                responder.Respond(_cam.Picture);
            }
            else
            {
                var fileResponse = GetFileResponse(Directories.Config, ImageNotAvailable);
                SendResponse(fileResponse, responder);
            }
        }

        /// <summary>
        /// Returns the time the camera last captured an image
        /// </summary>
        private void WebEvent_CameraTimestamp(string path, WebServer.HttpMethod method, Responder responder)
        {
            var time = _cam.PictureLastTaken;
            var response = GetJsonReponse(time);
            SendResponse(response, responder);
        }

        /// <summary>
        /// Returns the total images taken in a directory including subdirectories
        /// </summary>
        private void WebEvent_GalleryCount(string path, WebServer.HttpMethod method, Responder responder)
        {
            var queriedFolderPath = responder.QueryString(QueryStrings.Directory);
            var folderPath = MyPath.Combine(Directories.Camera, queriedFolderPath);
            var totalFiles = _fm.CountFiles(folderPath, true);
            var response = GetJsonReponse(totalFiles);
            SendResponse(response, responder);
        }

        /// <summary>
        /// Returns an stored image on the MicroSD card
        /// </summary>
        private void WebEvent_GalleryImage(string path, WebServer.HttpMethod method, Responder responder)
        {
            var filePath = responder.QueryString(QueryStrings.File);
            var fileResponse = GetFileResponse(Directories.Camera, filePath);
            if (!fileResponse.Found)
            {
                fileResponse = GetFileResponse(Directories.Config, ImageNotAvailable);
            }

            SendResponse(fileResponse, responder);
        }

        /// <summary>
        /// Returns the contents of the requested directory as JSON
        /// </summary>
        private void WebEvent_GalleryList(string path, WebServer.HttpMethod method, Responder responder)
        {
            var folderPath = responder.QueryString(QueryStrings.Directory);
            var recursive = responder.QueryBoolean(QueryStrings.Recursive);

            if (!_fm.HasFileSystem())
            {
                responder.NotFound();
                return;
            }

            // Check area exists on the device
            if (!_fm.RootDirectoryExists(Directories.Camera))
            {
                responder.NotFound();
                return;
            }

            // Define the full directory
            var systemPath = MyPath.Combine(Directories.Camera, folderPath);

            if (!_fm.DirectoryExists(systemPath))
            {
                responder.NotFound();
                return;
            }

            // Get file list
            string[] files = recursive
                ? _fm.ListFilesRecursive(systemPath).ToStringArray()
                : _fm.ListFiles(systemPath);

            var list = new ArrayList();
            foreach (var file in files)
            {
                var leaf = GalleryObject.FromPath(Directories.Camera, file, WebRoutes.GalleryImage + "?file=");
                list.Add(leaf);
            }

            var response = GetJsonReponse(list);
            SendResponse(response, responder);
        }

        /// <summary>
        /// Returns the total system uptime
        /// </summary>
        private void WebEvent_SystemUptime(string path, WebServer.HttpMethod method, Responder responder)
        {
            var response = GetJsonReponse(_sys.Uptime);
            SendResponse(response, responder);
        }

        private void WebEvent_WeatherLuminosity(string path, WebServer.HttpMethod method, Responder responder)
        {
            var response = GetJsonReponse(_we.Luminosity);
            SendResponse(response, responder);
        }

        private void WebEvent_WeatherHumidity(string path, WebServer.HttpMethod method, Responder responder)
        {
            var response = GetJsonReponse(_we.Humidity);
            SendResponse(response, responder);
        }

        private void WebEvent_WeatherTemperature(string path, WebServer.HttpMethod method, Responder responder)
        {
            var response = GetJsonReponse(_we.Temperature);
            SendResponse(response, responder);
        }
    }
}
