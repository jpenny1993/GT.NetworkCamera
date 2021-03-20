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
using MyHome.Utilities;
    
namespace MyHome
{
    public partial class Program
    {
        private bool _savingPicture;
        private GT.Timer _timer;
        private CameraManager _cameraManager;
        private FileManager _fileManager;
        private NetworkManager _networkManager;
        private SystemManager _systemManager;
        private WeatherManager _weatherManager;
        private WebsiteManager _websiteManager;

        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            // DO NOT PUT BLOCKING CODE IN THE MAIN THREAD
            Debug.Print("Startup Initiated");

            SetupDevices();

            // Create timer to action events on a loop
            _timer = new GT.Timer(60000); // every 60 seconds
            _timer.Tick += Update_Tick;
            _timer.Start();

            Debug.Print("Startup Complete");
        }

        private void SetupDevices()
        {
            _systemManager = new SystemManager();

            multicolorLED.TurnRed();

            _fileManager = new FileManager(sdCard);
            _fileManager.Remount(); // TODO: make non-blocking call

            _networkManager = new NetworkManager(ethernetJ11D);
            _networkManager.OnStatusChanged += NetworkManager_OnStatusChanged;
            _networkManager.ModeStatic("192.168.2.2");
            // _networkManager.ModeDhcp();
            _networkManager.Enable();

            _cameraManager = new CameraManager(camera, _systemManager);
            _cameraManager.OnPictureTaken += CameraManager_OnPictureTaken;

            button.ButtonReleased += Button_ButtonReleased;
            button.TurnLedOff();

            _weatherManager = new WeatherManager(tempHumidity);
            _weatherManager.Start();

            _websiteManager = new WebsiteManager(_systemManager, _cameraManager, _fileManager, _weatherManager);
        }

        private void Button_ButtonReleased(Button sender, Button.ButtonState state)
        {
            button.ToggleLED();
        }

        private void CameraManager_OnPictureTaken(GT.Picture picture)
        {
            _savingPicture = true;

            var now = DateTime.Now;
            var dateDirectory = now.ToString("yyMMdd");
            var filename = string.Concat("IMG_", now.ToString("yyMMddHHmmss"), FileExtensions.Bitmap);

            var filepath = Path.Combine(Directories.Camera, dateDirectory, filename);
            _fileManager.SaveFile(filepath, picture);
            _savingPicture = false;
            var test = new DictionaryEntry(1, "");
        }

        private void NetworkManager_OnStatusChanged(NetworkStatus status, NetworkStatus previousStatus)
        {
            if (previousStatus == NetworkStatus.NetworkAvailable) 
            {
                _websiteManager.Stop();
            }

            switch(status) 
            {
                case NetworkStatus.Disabled:
                    multicolorLED.TurnRed();
                    break;
                case NetworkStatus.Enabled:
                    multicolorLED.TurnColor(GT.Color.Orange);
                    break;
                case NetworkStatus.NetworkStuck:
                    multicolorLED.BlinkRepeatedly(GT.Color.Yellow);
                    break;
                case NetworkStatus.NetworkDown:
                    multicolorLED.TurnColor(GT.Color.Yellow);
                    break;
                case NetworkStatus.NetworkUp:
                    multicolorLED.BlinkRepeatedly(GT.Color.Green);
                    break;
                case NetworkStatus.NetworkAvailable:
                    // Calculates the correct uptime if Internet connection is lost while running
                    multicolorLED.BlinkRepeatedly(GT.Color.Blue);
                    DateTime timeBeforeSync = _systemManager.Time;
                    if (false)// Time.SyncInternetTime()) // TODO REFACTOR
                    {
                        var now = _systemManager.Time;
                        var recalculatedStartTime = now - (timeBeforeSync - _systemManager.StartTime);
                        _systemManager.SetSystemStartTime(recalculatedStartTime);
                        Debug.Print("Synchronised time: " + FormatDateTime(now));
                        Debug.Print("Recalculated uptime: " + FormatTimeSpan(_systemManager.Uptime));
                        multicolorLED.TurnColor(GT.Color.Blue);
                    }
                    else 
                    {
                        // Unable to sync time
                        multicolorLED.TurnGreen();
                    }

                    _websiteManager.Start(_networkManager.IpAddress);
                    break;
            }
        }

        private void TakeSnapshot()
        {
            if (button.IsLedOn && !_savingPicture && _cameraManager.Ready)
            {
                _cameraManager.TakePicture();
            }
        }

        private void Update_Tick(GT.Timer timer)
        {
            Debug.Print("Tick: " + FormatTimeSpan(_systemManager.Uptime));
            TakeSnapshot();
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
