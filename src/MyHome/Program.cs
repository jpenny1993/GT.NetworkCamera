using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using MyHome.Constants;
using MyHome.Modules;
    
namespace MyHome
{
    public partial class Program
    {
        private DateTime _start;
        private GT.Timer _timer;
        private CameraManager _cameraManager;
        private FileManager _fileManager;
        private NetworkManager _networkManager;
        private WebEvent[] _webEvents;

        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            // DO NOT PUT BLOCKING CODE IN THE MAIN THREAD
            Debug.Print("Startup Initiated");
            _start = DateTime.Now;

            SetupDevices();

            // Create timer to action events on a loop
            _timer = new GT.Timer(60000); // every 60 seconds
            _timer.Tick += Update_Tick;
            _timer.Start();

            Debug.Print("Startup Complete");
        }


        private void SetupDevices()
        {
            multicolorLED.TurnRed();

            _fileManager = new FileManager(microSDCard);
            _fileManager.Remount(); // TODO: make non-blocking call

            _networkManager = new NetworkManager(ethernetJ11D);
            _networkManager.OnStatusChanged += NetworkManager_OnStatusChanged;
            _networkManager.Enable();

            _cameraManager = new CameraManager(camera);
            _cameraManager.OnPictureTaken += CameraManager_OnPictureTaken;

            button.ButtonReleased += Button_ButtonReleased;
            button.TurnLedOn();

        }

        private void Button_ButtonReleased(Button sender, Button.ButtonState state)
        {
            TakeSnapshot();
        }

        private void CameraManager_OnPictureTaken(GT.Picture picture)
        {
            // TODO: implement path.combine, and accessible constants for directories
            var filepath = string.Concat(Directories.Camera, "\\", "IMG_", DateTime.Now.ToString("yyMMdd_HHmmss"), ".bmp");
            _fileManager.SaveFile(filepath, picture);

            button.TurnLedOn();
        }

        private void NetworkManager_OnStatusChanged(NetworkStatus status, NetworkStatus previousStatus)
        {
            if (previousStatus == NetworkStatus.NetworkAvailable) 
            {
                WebServer.StopLocalServer();
            }

            switch(status) 
            {
                case NetworkStatus.Disabled:
                    multicolorLED.TurnRed();
                    break;
                case NetworkStatus.Enabled:
                    multicolorLED.BlinkRepeatedly(GT.Color.Orange);
                    break;
                case NetworkStatus.NetworkDown:
                    multicolorLED.TurnColor(GT.Color.Orange);
                    break;
                case NetworkStatus.NetworkUp:
                    multicolorLED.BlinkRepeatedly(GT.Color.Green);
                    break;
                case NetworkStatus.NetworkAvailable:
                    // Calculates the correct uptime if Internet connection is lost while running
                    multicolorLED.BlinkRepeatedly(GT.Color.Blue);
                    DateTime timeBeforeSync = DateTime.Now;
                    if (Time.SyncInternetTime()) // TODO REFACTOR
                    {
                        var now = DateTime.Now;
                        _start = now - (timeBeforeSync - _start);
                        Debug.Print("Synchronised time: " + FormatDateTime(now));
                        Debug.Print("Recalculated uptime: " + FormatTimeSpan(GetUptime()));
                        multicolorLED.TurnColor(GT.Color.Blue);
                    }
                    else 
                    {
                        // Unable to sync time
                        multicolorLED.TurnGreen();
                    }
                    WebServer_Setup();
                    break;
            }
        }

        private TimeSpan GetUptime()
        {
            return DateTime.Now - _start;
        }

        private void TakeSnapshot()
        {
            if (_cameraManager.Ready && button.IsLedOn)
            {
                button.TurnLedOff();
                _cameraManager.TakePicture();
            }
        }

        private void Update_Tick(GT.Timer timer)
        {
            Debug.Print("Tick: " + FormatTimeSpan(GetUptime()));
            TakeSnapshot();
        }

        private void WebServer_Setup()
        {
            Debug.Print("Starting web server");
            try
            {
                WebServer.StartLocalServer(ethernetJ11D.NetworkSettings.IPAddress, 80);
                Debug.Print("Started web server");

                Debug.Print("Registering Index Page");
                WebServer.DefaultEvent.WebEventReceived += WebEvent_Index;

                Debug.Print("Registering Image Page");
                var webEvents = WebServer.SetupWebEvent("image");
                webEvents.WebEventReceived += WebEvent_Image;
                Debug.Print("Completed web server startup");
            }
            catch (Exception ex)
            {
                Debug.Print("Failed to start web server... " + ex.ToString());
                multicolorLED.TurnColor(GT.Color.Purple);
            }
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
            if (_cameraManager.HasPicture)
            {
                responder.Respond(_cameraManager.Picture);
                return;
            }
            
            responder.Respond("Latest image is not yet available.");
        }

        private static string FormatInteger(int value)
        {
            return value > -1 && value < 10
                ? "0" + value.ToString()
                : value.ToString();
        }

        private static string FormatDateTime(DateTime datetime)
        {
            return datetime.ToString("dd/MM/yy HH:mm:ss");
        }

        private static string FormatTimeSpan(TimeSpan timespan)
        {
            const string colon = ":";
            return string.Concat(
                FormatInteger(timespan.Days), colon,
                FormatInteger(timespan.Hours), colon,
                FormatInteger(timespan.Minutes), colon,
                FormatInteger(timespan.Seconds)
            );
        }

    }
}
