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
            if (_picture != null)
            {
                responder.Respond(_picture);
            }
            else
            {
                var fileResponse = GetFileResponse(Directories.Config, ImageNotAvailable);
                SendResponse(fileResponse, responder);
            }
        }

        /// <summary>
        /// Returns the total images taken in a directory including subdirectories
        /// </summary>
        private void WebEvent_GalleryCount(string path, WebServer.HttpMethod method, Responder responder)
        {
            var queriedFolderPath = responder.QueryParameter(QueryStrings.Directory);
            var folderPath = Path.Combine(Directories.Camera, queriedFolderPath);
            var totalFiles = _fm.CountFiles(folderPath, true);
            responder.Respond(totalFiles.ToString());
        }

        /// <summary>
        /// Returns an stored image on the MicroSD card
        /// </summary>
        private void WebEvent_GalleryImage(string path, WebServer.HttpMethod method, Responder responder)
        {
            var fileResponse = GetFileResponse(Directories.Camera, path);
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
            var folderPath = responder.QueryParameter(QueryStrings.Directory);
            var response = BrowseDirectoryResponse(Directories.Camera, folderPath);
            SendResponse(response, responder);
        }
    }
}
