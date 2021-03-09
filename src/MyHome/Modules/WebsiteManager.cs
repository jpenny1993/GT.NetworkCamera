using System;
using System.Collections;
using Microsoft.SPOT;

using Gadgeteer.Networking;
using GT = Gadgeteer;

namespace MyHome.Modules
{
    public sealed class WebsiteManager
    {
        private GT.Picture _picture;

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
            Debug.Print("Starting web server");
            WebServer.StartLocalServer(ipAddress, port);

            RegisterWebEvents();

            Debug.Print("Completed web server startup");
        }

        public void Stop()
        {
            WebServer.StopLocalServer();
        }

        public void UpdatePicture(GT.Picture picture)
        {
            _picture = picture;
        }

        private void WebEvent_Index(string path, WebServer.HttpMethod method, Responder responder)
        {
            /*
             // x.x.x.x:80/route?query=value
             if (responder.UrlParameters.Contains("query"))
             {
                 if (responder.UrlParameters["query"].ToString() == "value")
                 { 
                 }
             }
             */
            responder.Respond("Hello World");
        }

        private void WebEvent_Image(string path, WebServer.HttpMethod method, Responder responder)
        {
            if (_picture != null)
                responder.Respond(_picture);
            else
                responder.Respond("Latest image is not yet available.");
        }
    }
}
