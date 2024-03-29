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
using Microsoft.SPOT.Input;
    
namespace MyHome
{
    public partial class Program
    {
        private static readonly string GlobalConfigFilePath = Path.Combine(Directories.Config, "system.xml");

        private static bool IsFirstLoad = true;

        private GlobalConfiguration Configuration;

        private GT.Timer _eventTimer;
        private GT.Color _prevColour;
        private Logger _logger;
        private CameraManager _cameraManager;
        private DisplayManager _displayManager;
        private FileManager _fileManager;
        private LightManager _lightManager;
        private NetworkManager _networkManager;
        private SystemManager _systemManager;
        private WeatherManager _weatherManager;
        private WebsiteManager _websiteManager;
        private AttendanceManager _attendanceManager;

        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            Debug.Print("Startup Initiated");
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
            _lightManager = new LightManager(networkLED, infoLED);
            _lightManager.NetworkLED.TurnRed();

            _systemManager = new SystemManager();
            _displayManager = new DisplayManager(displayT35, _systemManager);

            _systemManager.OnTimeSynchronised += SystemManager_OnTimeSynchronised;

            _fileManager = new FileManager(sdCard);

            _attendanceManager = new AttendanceManager(rfidReader, _systemManager, _fileManager);
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
                else if (diskInserted)
                {
                    // Re-enable file logging on disk restoration
                    Logger.SetupFileLogging(
                        Configuration.Logging.FileLogsEnabled,
                        Configuration.Logging.ConsoleLogsEnabled);
                }
                else
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

            _networkManager.UpdateNetworkStatus();

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
                    _displayManager.ShowDashboard(
                       _systemManager.Time,
                       _networkManager.IpAddress,
                       _weatherManager.Humidity,
                       _weatherManager.Luminosity,
                       _weatherManager.Temperature,
                       _fileManager.TotalFreeSpaceInMb);
                }

                _lightManager.CheckScheduleForLightsOut(_systemManager.Time);
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

            // Returns the state to dashboard after 5 seconds
            _displayManager.RefreshState(_systemManager.Uptime);

            if (_displayManager.IsReadyForScreenWake)
            {
                _logger.Print("Triggering screen wake-up events");
                _displayManager.ShowDashboard(
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
                    _lightManager.NetworkLED.TurnRed();
                    _displayManager.ShowStatusNotification("Network disabled");
                    break;
                case NetworkStatus.Enabled:
                    _lightManager.NetworkLED.TurnOrange();
                    _displayManager.ShowStatusNotification("Ethernet cable disconnected");
                    break;
                case NetworkStatus.NetworkStuck:
                    _lightManager.NetworkLED.BlinkYellow();
                    _displayManager.ShowStatusNotification("Reconnect ethernet cable");
                    break;
                case NetworkStatus.NetworkDown:
                    _lightManager.NetworkLED.TurnYellow();
                    _displayManager.ShowStatusNotification("Ethernet cable connected");
                    break;
                case NetworkStatus.NetworkUp:
                    _lightManager.NetworkLED.BlinkGreen();
                    _displayManager.ShowStatusNotification("Waiting for an IP Address");
                    break;
                case NetworkStatus.NetworkAvailable:
                    _lightManager.NetworkLED.TurnGreen();
                    if (!_systemManager.HasTimeSyncronised && IsFirstLoad)
                    {
                        _displayManager.ShowStatusNotification("Waiting for time to synchronise");
                        _lightManager.InfoLED.BlinkBlue();
                        _systemManager.SyncroniseInternetTime();
                    }
                    else 
                    {
                        _displayManager.ShowStatusNotification("Network online");
                    }
                    _websiteManager.Start(_networkManager.IpAddress);
                    break;
            }
        }

        private void AttendanceManager_OnAccessDenied()
        {
            _displayManager.ShowAccessDenied();
            _lightManager.InfoLED.WinkRed();
        }

        private void AttendanceManager_OnScannedKeycard(DateTime timestamp, string attendanceStatus, string rfid, string displayName)
        {
            var isWorkingHours = _attendanceManager.IsWithinWorkingHours(timestamp);

            _lightManager.InfoLED.WinkGreen();
            
            switch (attendanceStatus)
            {
                case AttendanceStatus.ClockIn:
                    {
                        var isOpeningGracePeriod = _attendanceManager.IsWithinOpeningGracePeriod(timestamp);
                        var hasClockedInToday = _attendanceManager.HasUserClockedInOnDate(timestamp, rfid);

                        if (isOpeningGracePeriod)
                        {
                            // Good boys get no prompts
                            _displayManager.ShowClockInOrOut(timestamp, attendanceStatus, displayName);
                            _attendanceManager.ClockIn(timestamp, rfid,  "On time");
                        }
                        if (hasClockedInToday && isWorkingHours)
                        {
                            _displayManager.ShowClockInOrOut(timestamp, attendanceStatus, displayName);
                            _attendanceManager.ClockIn(timestamp, rfid, "Returning from break");
                        }
                        else if (isWorkingHours)
                        {
                            // Bad boys get the late screen
                            _displayManager.ShowClockInOrOutPrompt(
                                timestamp,
                                attendanceStatus,
                                displayName,
                                "You've arrived late, please confirm to clock-in",
                                new TouchEventHandler((sender, args) =>
                                {
                                    _displayManager.ShowClockInOrOut(timestamp, attendanceStatus, displayName);
                                    _attendanceManager.ClockIn(timestamp, rfid, "Late");
                                }),
                                new TouchEventHandler((sender, args) => _displayManager.ReturnToDashboard()));
                        }
                        else
                        {
                            // Out of hours boys get the overtime screen
                            _displayManager.ShowClockInOrOutPrompt(
                                timestamp,
                                attendanceStatus,
                                displayName,
                                "Its out of hours, please confirm to clock-in",
                                new TouchEventHandler((sender, args) =>
                                {
                                    _displayManager.ShowClockInOrOut(timestamp, attendanceStatus, displayName);
                                    _attendanceManager.ClockIn(timestamp, rfid, "Out of hours");
                                }),
                                new TouchEventHandler((sender, args) => _displayManager.ReturnToDashboard()));
                        }

                        _displayManager.TouchScreen();
                        _displayManager.EnableBacklight();
                    }
                    break;

                case AttendanceStatus.ClockOut:
                    {
                        var isClosingGracePeriod = _attendanceManager.IsWithinClosingGracePeriod(timestamp);
                        if (isClosingGracePeriod)
                        {
                            _displayManager.ShowClockInOrOut(timestamp, attendanceStatus, displayName);
                            _attendanceManager.ClockOut(timestamp, rfid, "On time");
                        }
                        else if (isWorkingHours)
                        {
                            _displayManager.ShowClockInOrOut(timestamp, attendanceStatus, displayName);
                            _attendanceManager.ClockOut(timestamp, rfid, "Break-time");
                        }
                        else
                        {
                            _displayManager.ShowClockInOrOutPrompt(
                                timestamp,
                                attendanceStatus,
                                displayName,
                                "Its out of hours, please confirm to clock-out",
                                new TouchEventHandler((sender, args) =>
                                {
                                    _displayManager.ShowClockInOrOut(timestamp, attendanceStatus, displayName);
                                    _attendanceManager.ClockOut(timestamp, rfid, "Overtime");
                                }),
                                new TouchEventHandler((sender, args) => _displayManager.ReturnToDashboard()));
                        }

                        _displayManager.TouchScreen();
                        _displayManager.EnableBacklight();
                    }
                    break;

                default: 
                    _logger.Warning("Unhandled attendance status \"{0}\"", attendanceStatus);
                    break;
            }
        }

        private void SystemManager_OnTimeSynchronised(bool synchronised)
        {
            if (synchronised)
            {
                if (IsFirstLoad)
                { 
                    _lightManager.InfoLED.TurnBlue();
                    _displayManager.ShowStatusNotification("Waiting for sensor readings");
                    _weatherManager.TakeMeasurement();
                }
            }
            else
            {
                _displayManager.ShowStatusNotification("Unable to synchronise time");
                _lightManager.InfoLED.TurnRed();
            }
        }

        private void WeatherManager_OnMeasurement(WeatherModel weather)
        {
            if (!_systemManager.HasTimeSyncronised) return;

            if (IsFirstLoad)
            {
                IsFirstLoad = false;
                _displayManager.SwitchToDashboard();
                _displayManager.TouchScreen();
                _displayManager.ShowDashboard(
                      _systemManager.Time,
                      _networkManager.IpAddress,
                      _weatherManager.Humidity,
                      _weatherManager.Luminosity,
                      _weatherManager.Temperature,
                      _fileManager.TotalFreeSpaceInMb);
                _lightManager.InfoLED.TurnOff();
            }

            _weatherManager.SaveMeasurementToSdCard(_fileManager, weather, _systemManager.Time);
        }
    }
}
