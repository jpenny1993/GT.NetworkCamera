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
        private GT.Color _prevColour;
        private Logger _logger;
        private CameraManager _cameraManager;
        private DisplayManager _displayManager;
        private FileManager _fileManager;
        private NetworkManager _networkManager;
        private SystemManager _systemManager;
        private WeatherManager _weatherManager;
        private WebsiteManager _websiteManager;
        private SecurityManager _securityManager;

        private IAwaitable _saveMeasurementThread = Awaitable.Default;
        private IAwaitable _savePictureThread = Awaitable.Default;

        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            Debug.Print("Startup Initiated");
            networkLED.TurnRed();
            _logger = Logger.ForContext(this);

            SetupDevices();

            // Create timer to trigger events once the network time has been synchronised
            _timer = new GT.Timer(1000); // Occurs every second
            _timer.Tick += Update_Tick;
            _logger.Information("Startup Complete");
        }

        private void Update_Tick(GT.Timer timer)
        {
            var utcNow = DateTime.UtcNow;

            // Every 30 seconds
            if (utcNow.Second % 30 == 0)
            {
                _logger.Information("Triggering events [30 seconds]");
                _weatherManager.TakeMeasurement();
                _displayManager.DismissBacklight();
            }

            // Every 60 seconds
            if (utcNow.Second == 0)
            {
                _logger.Information("Triggering events [60 seconds]");
                _displayManager.ShowDashboard(
                   DateTime.Now,
                   _networkManager.IpAddress,
                   _weatherManager.Humidity,
                   _weatherManager.Luminosity,
                   _weatherManager.Temperature,
                   _fileManager.TotalFreeSpaceInMb);
            }

            // Every 5 minutes
            if (utcNow.Minute % 5 == 0 && utcNow.Second == 0)
            {
                _logger.Information("Triggering events [5 minutes]");
                TakeSnapshot();
            }

            // At 3 AM every morning
            if (utcNow.Hour == 3 && utcNow.Minute == 0 && utcNow.Second == 0)
            {
                _logger.Information("Triggering events [3 AM]");
                _systemManager.SyncroniseInternetTime();
            }
        }

        private void SetupDevices()
        {
            _displayManager = new DisplayManager(displayT35);
            _systemManager = new SystemManager();
            _systemManager.OnTimeSynchronised += SystemManager_OnTimeSynchronised;

            _fileManager = new FileManager(sdCard);
            _securityManager = new SecurityManager(rfidReader, _fileManager);
            _securityManager.OnAccessDenied += SecurityManager_OnAccessDenied;
            _securityManager.OnAccessGranted += SecurityManager_OnAccessGranted;
            _securityManager.OnScanEnabled += SecurityManager_OnScanEnabled;
            _securityManager.OnScanCompleted += SecurityManager_OnScanCompleted;

            Logger.Initialise(_fileManager);

            _fileManager.OnDeviceSwap += (bool diskInserted) =>
            {
                Logger.SetupFileLogging(diskInserted);

                if (diskInserted)
                {
                    _securityManager.Initialise();
                }
            };

            // fix SD card mount being unreliable on startup
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
                    networkLED.TurnRed();
                    break;
                case NetworkStatus.Enabled:
                    networkLED.TurnColor(GT.Color.Orange);
                    break;
                case NetworkStatus.NetworkStuck:
                    networkLED.BlinkRepeatedly(GT.Color.Yellow);
                    break;
                case NetworkStatus.NetworkDown:
                    networkLED.TurnColor(GT.Color.Yellow);
                    break;
                case NetworkStatus.NetworkUp:
                    networkLED.BlinkRepeatedly(GT.Color.Green);
                    break;
                case NetworkStatus.NetworkAvailable:
                    networkLED.TurnGreen();
                    infoLED.BlinkRepeatedly(GT.Color.Blue);
                    _systemManager.SyncroniseInternetTime();
                    _websiteManager.Start(_networkManager.IpAddress);
                    break;
            }
        }

        private void SecurityManager_OnAccessDenied()
        {
            _logger.Information("RFID login failed");
            _prevColour = infoLED.GetCurrentColor();
            infoLED.BlinkOnce(GT.Color.Red, new TimeSpan(0, 0, 3), _prevColour);
        }

        private void SecurityManager_OnAccessGranted(string username)
        {
            _logger.Information("Hello {0}", username);
            _prevColour = infoLED.GetCurrentColor();
            infoLED.BlinkOnce(GT.Color.Green, new TimeSpan(0, 0, 3), _prevColour);
        }

        private void SecurityManager_OnScanCompleted(bool timeoutOccurred)
        {
            if (timeoutOccurred)
            {
                _logger.Information("RFID scan timed out");
                infoLED.BlinkOnce(GT.Color.Magenta, new TimeSpan(0, 0, 3), _prevColour);
            }
            else
            { 
                _logger.Information("RFID user scanned");
                infoLED.BlinkOnce(GT.Color.Green, new TimeSpan(0, 0, 3), _prevColour);
            }
        }

        private void SecurityManager_OnScanEnabled()
        {
            _logger.Information("RFID scan enabled");
            _prevColour = infoLED.GetCurrentColor();
            infoLED.TurnColor(GT.Color.Magenta);
        }

        private void SystemManager_OnTimeSynchronised(bool synchronised)
        {
            if (synchronised)
            {
                infoLED.TurnColor(GT.Color.Blue);
                _weatherManager.TakeMeasurement();
                _displayManager.TouchScreen();
                _timer.Start();
            }
            else
            {
                infoLED.TurnRed();
            }
        }

        private void TakeSnapshot()
        {
            if (button.IsLedOn && !_savePictureThread.IsRunning)
            {
                _cameraManager.TakePicture();
            }
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
