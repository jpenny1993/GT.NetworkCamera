﻿using System;
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
using MyHome.Configuration;
    
namespace MyHome
{
    public partial class Program
    {
        private static readonly string GlobalConfigFilePath = Path.Combine(Directories.Config, "system.xml");

        private GlobalConfiguration Configuration;

        private GT.Timer _eventTimer;
        private GT.Color _prevColour;
        private Logger _logger;
        private CameraManager _cameraManager;
        private DisplayManager _displayManager;
        private FileManager _fileManager;
        private NetworkManager _networkManager;
        private SystemManager _systemManager;
        private WeatherManager _weatherManager;
        private WebsiteManager _websiteManager;
        private AttendanceManager _attendanceManager;

        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            Debug.Print("Startup Initiated");
            networkLED.TurnRed();
            _logger = Logger.ForContext(this);

            SetupDevices();

            const int OneSecondInterval = 1000;
            _eventTimer = new GT.Timer(OneSecondInterval);
            _eventTimer.Tick += EventTimer_Tick;
            _eventTimer.Start();
            _logger.Information("Startup Complete");
        }

        private void ReadGlobalConfigurationFile()
        {
            if (!_fileManager.FileExists(GlobalConfigFilePath))
            {
                using (var fs = _fileManager.GetFileStream(GlobalConfigFilePath, FileMode.CreateNew, FileAccess.Write))
                {
                    Configuration = GlobalConfiguration.DefaultConfiguration;
                    GlobalConfiguration.Write(fs, Configuration);
                }
            }
            else
            {
                using (var fs = _fileManager.GetFileStream(GlobalConfigFilePath, FileMode.Open, FileAccess.Read))
                {
                    Configuration = GlobalConfiguration.Read(fs);
                }
            }
        }

        private void SetupDevices()
        {
            _displayManager = new DisplayManager(displayT35);

            _systemManager = new SystemManager();
            _systemManager.OnTimeSynchronised += SystemManager_OnTimeSynchronised;

            _fileManager = new FileManager(sdCard);

            _attendanceManager = new AttendanceManager(rfidReader, _fileManager);
            _attendanceManager.OnAccessDenied += AttendanceManager_OnAccessDenied;
            _attendanceManager.OnScannedKeycard += AttendanceManager_OnScannedKeycard;

            _networkManager = new NetworkManager(ethernetJ11D);
            _networkManager.OnStatusChanged += NetworkManager_OnStatusChanged;

            _cameraManager = new CameraManager(camera, _systemManager);
            _cameraManager.OnPictureTaken += CameraManager_OnPictureTaken;

            button.ButtonReleased += Button_ButtonReleased;
            button.TurnLedOff();

            _weatherManager = new WeatherManager(tempHumidity, lightSense);
            _weatherManager.OnMeasurement += WeatherManager_OnMeasurement;

            _websiteManager = new WebsiteManager(_systemManager, _cameraManager, _fileManager, _weatherManager);

            Logger.Initialise(_fileManager);

            // Code to trigger once the SD card is ready
            _fileManager.OnDeviceSwap += (bool diskInserted) =>
            {
                // First time global configuration
                if (diskInserted && Configuration == null)
                {
                    ReadGlobalConfigurationFile();

                    Logger.SetupFileLogging(
                        Configuration.Logging.FileLogsEnabled,
                        Configuration.Logging.ConsoleLogsEnabled);

                    _attendanceManager.Initialise(Configuration.Attendance);
                    _cameraManager.Initialise(Configuration.Camera);
                    _weatherManager.Initialise(Configuration.Sensors);
                    _networkManager.Initialise(Configuration.Network);
                }
                else if (!diskInserted)
                {
                    // Disable file logging on disk removal
                    Logger.SetupFileLogging(false);
                }
            };

            // Fix SD card mount being unreliable on startup
            _fileManager.Remount();
        }

        private void EventTimer_Tick(GT.Timer timer)
        {
            var time = _systemManager.UtcTime;

            if (time.Second % 5 == 0)
            {
                _logger.Print("Triggering events [5th sec]");
                _networkManager.UpdateNetworkStatus();
            }

            // There is no point running any other events until time has been synchronised once
            if (!_systemManager.HasTimeSyncronised) return;

            if (time.Second % 30 == 0)
            {
                _logger.Print("Triggering events [30th sec]");
                _weatherManager.TakeMeasurement();
            }

            if (time.Second == 0)
            {
                _logger.Print("Triggering events [60th sec]");
                if (_displayManager.IsDisplayActive)
                {
                    _displayManager.UpdateDashboard(
                       _systemManager.Time,
                       _networkManager.IpAddress,
                       _weatherManager.Humidity,
                       _weatherManager.Luminosity,
                       _weatherManager.Temperature,
                       _fileManager.TotalFreeSpaceInMb);
                }
            }

            if (time.Minute % 5 == 0 && time.Second == 0)
            {
                _logger.Print("Triggering events [5th min]");
                _cameraManager.TakePicture(_systemManager.Time);
            }

            if (time.Minute % 58 == 0 && time.Second == 0)
            {
                _logger.Print("Triggering events [58th min]");
                _attendanceManager.AutoClockOut(_systemManager.Date, _systemManager.Time);
            }

            // At 3 AM UTC every morning
            if (time.Hour == 3 && time.Minute == 0 && time.Second == 0)
            {
                _logger.Print("Triggering events [Maintenance]");
                _systemManager.SyncroniseInternetTime();
            }

            // Keep screen active until first sensor reading has been done
            if (_displayManager.IsLoadingScreen) return;

            if (_displayManager.IsReadyForScreenWake)
            {
                _logger.Print("Triggering screen wake-up events");
                _displayManager.UpdateDashboard(
                   _systemManager.Time,
                   _networkManager.IpAddress,
                   _weatherManager.Humidity,
                   _weatherManager.Luminosity,
                   _weatherManager.Temperature,
                   _fileManager.TotalFreeSpaceInMb);

                _displayManager.EnableBacklight();
            }
            else if (_displayManager.IsReadyForScreenTimeout)
            {
                _logger.Print("Triggering screen timeout events");
                _displayManager.DismissBacklight();
            }
        }

        private void Button_ButtonReleased(Button sender, Button.ButtonState state)
        {
            button.ToggleLED();
        }

        private void CameraManager_OnPictureTaken(GT.Picture picture)
        {
            _cameraManager.SavePictureToSdCard(_fileManager, picture, _systemManager.Time);
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
                    _displayManager.UpdateStatus("Network disabled");
                    break;
                case NetworkStatus.Enabled:
                    networkLED.TurnColor(GT.Color.Orange);
                    _displayManager.UpdateStatus("Ethernet cable disconnected");
                    break;
                case NetworkStatus.NetworkStuck:
                    networkLED.BlinkRepeatedly(GT.Color.Yellow);
                    _displayManager.UpdateStatus("Network stuck, reconnect ethernet cable");
                    break;
                case NetworkStatus.NetworkDown:
                    networkLED.TurnColor(GT.Color.Yellow);
                    _displayManager.UpdateStatus("Ethernet cable connected");
                    break;
                case NetworkStatus.NetworkUp:
                    networkLED.BlinkRepeatedly(GT.Color.Green);
                    _displayManager.UpdateStatus("Network up, waiting for IP Address");
                    break;
                case NetworkStatus.NetworkAvailable:
                    networkLED.TurnGreen();
                    infoLED.BlinkRepeatedly(GT.Color.Blue);
                    _displayManager.UpdateStatus("Network online, synchronising time");
                    if (!_systemManager.HasTimeSyncronised)
                    {
                        _systemManager.SyncroniseInternetTime();
                    }
                    _websiteManager.Start(_networkManager.IpAddress);
                    break;
            }
        }

        private void AttendanceManager_OnAccessDenied()
        {
            _logger.Information("RFID login failed");
            _prevColour = infoLED.GetCurrentColor();
            infoLED.BlinkOnce(GT.Color.Red, new TimeSpan(0, 0, 3), _prevColour);
        }

        private void AttendanceManager_OnScannedKeycard(string rfid, string displayName, string status)
        {
            _prevColour = infoLED.GetCurrentColor();
            infoLED.BlinkOnce(GT.Color.Green, new TimeSpan(0, 0, 3), _prevColour);

            switch (status)
            {
                case AttendanceStatus.ClockIn:
                    _logger.Information("Hello {0}", displayName);
                    _attendanceManager.ClockIn(_systemManager.Time, rfid);
                    break;

                case AttendanceStatus.ClockOut:
                    _logger.Information("Goodbye {0}", displayName);
                    _attendanceManager.ClockOut(_systemManager.Time, rfid);
                    break;

                default: 
                    _logger.Warning("Unhandled attendance status \"{0}\"", status);
                    break;
            }
        }

        private void SystemManager_OnTimeSynchronised(bool synchronised)
        {
            if (synchronised)
            {
                infoLED.TurnColor(GT.Color.Blue);
                if (_displayManager.IsLoadingScreen)
                { 
                    _displayManager.UpdateStatus("Waiting for sensor readings");
                    _weatherManager.TakeMeasurement();
                }
            }
            else
            {
                _displayManager.UpdateStatus("Unable to synchronise time");
                infoLED.TurnRed();
            }
        }

        private void WeatherManager_OnMeasurement(WeatherModel weather)
        {
            if (_displayManager.IsLoadingScreen)
            {
                _displayManager.EnableBacklight();
                _displayManager.TouchScreen();
            }

            _weatherManager.SaveMeasurementToSdCard(_fileManager, weather, _systemManager.Time);
        }
    }
}
