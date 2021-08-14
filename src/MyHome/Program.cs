using System;
using System.Collections;
using System.IO;
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
using Json.Lite;
using MyHome.Constants;
using MyHome.Extensions;
using MyHome.Modules;
using MyHome.Utilities;
using MyHome.Models;
    
namespace MyHome
{
    public partial class Program
    {
        private GT.Timer _timer;
        private Logger _logger;
        private CameraManager _cameraManager;
        private FileManager _fileManager;
        private NetworkManager _networkManager;
        private SystemManager _systemManager;
        private WeatherManager _weatherManager;
        private WebsiteManager _websiteManager;

        private IAwaitable _saveMeasurementThread = Awaitable.Default;
        private IAwaitable _savePictureThread = Awaitable.Default;

        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            // DO NOT PUT BLOCKING CODE IN THE MAIN THREAD
            Debug.Print("Startup Initiated");
            _logger = Logger.ForContext(this);

            SetupDevices();

            // Create timer to action events on a loop
            _timer = new GT.Timer(60000); // every 60 seconds
            _timer.Tick += Update_Tick;
            _timer.Start();

            _logger.Information("Startup Complete");
        }

        private void SetupDevices()
        {
            _systemManager = new SystemManager();
            _systemManager.OnTimeSynchronised += SystemManager_OnTimeSynchronised;

            multicolorLED.TurnRed();

            _fileManager = new FileManager(sdCard);
            Logger.Initialise(_fileManager);
            _fileManager.OnDeviceSwap += (bool diskInserted) =>
            {
                Logger.SetupFileLogging(diskInserted);
            };

            new Awaitable(() => _fileManager.Remount());

            _networkManager = new NetworkManager(ethernetJ11D);
            _networkManager.OnStatusChanged += NetworkManager_OnStatusChanged;
            //_networkManager.ModeStatic("192.168.1.69", gateway: "192.168.1.1");
            _networkManager.ModeDhcp();
            _networkManager.Enable();

            _cameraManager = new CameraManager(camera, _systemManager);
            _cameraManager.OnPictureTaken += CameraManager_OnPictureTaken;

            button.ButtonReleased += Button_ButtonReleased;
            button.TurnLedOff();

            _weatherManager = new WeatherManager(tempHumidity, lightSense);
            _weatherManager.OnMeasurement += WeatherManager_OnMeasurement;

            _websiteManager = new WebsiteManager(_systemManager, _cameraManager, _fileManager, _weatherManager);
        }

        private void Button_ButtonReleased(Button sender, Button.ButtonState state)
        {
            button.ToggleLED();
        }

        private void CameraManager_OnPictureTaken(GT.Picture picture)
        {
            var now = _systemManager.Time;
            var filename = string.Concat("IMG_", now.Timestamp(), FileExtensions.Bitmap);
            var filepath = MyPath.Combine(Directories.Camera, now.Datestamp(), filename);
            _savePictureThread = new Awaitable(() => _fileManager.SaveFile(filepath, picture));
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
                    multicolorLED.BlinkRepeatedly(GT.Color.Blue);
                    _systemManager.SyncroniseInternetTime();
                    _websiteManager.Start(_networkManager.IpAddress);
                    break;
            }
        }

        private void SystemManager_OnTimeSynchronised(bool synchronised)
        {
            if (synchronised)
            {
                multicolorLED.TurnColor(GT.Color.Blue);
                _weatherManager.Start();
            }
            else
            {
                multicolorLED.TurnGreen();
            }
        }

        private void TakeSnapshot()
        {
            if (button.IsLedOn &&
                _systemManager.IsTimeSynchronised &&
                !_savePictureThread.IsRunning)
            {
                _cameraManager.TakePicture();
            }
        }

        private void Update_Tick(GT.Timer timer)
        {
            _logger.Information("Tick: {0}", JsonConvert.SerializeObject(_systemManager.Uptime));
            TakeSnapshot();
        }

        private void WeatherManager_OnMeasurement(WeatherModel weather)
        {
            if (_saveMeasurementThread.IsRunning || !_fileManager.HasFileSystem()) { return; }

            _saveMeasurementThread = new Awaitable(() =>
            {
                var now = _systemManager.Time;
                var filename = string.Concat("Measurements_", now.Datestamp(), FileExtensions.Csv);
                var filepath = MyPath.Combine(Directories.Weather, filename);
                var fileExists = _fileManager.FileExists(filepath);

                using (var fs = _fileManager.GetFileStream(filepath, FileMode.Append, FileAccess.Write))
                {
                     // Setup column headers when creating a new CSV file
                    if (!fileExists)
                    {
                        _fileManager.WriteToFileStream(fs, "DateTime, Humidity, Luminosity, Temperature\r\n");
                    }

                    // Append line to existing CSV
                    _fileManager.WriteToFileStream(fs, 
                        "{0}, {1}, {2}, {3}\r\n".Format(
                            now.SortableDateTime(),
                            weather.Humidity,
                            weather.Luminosity,
                            weather.Temperature));
                }

                _logger.Information("{0} updated", filename);
            });
        }
    }
}
