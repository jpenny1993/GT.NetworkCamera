using System;
using System.Collections;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;
using MyHome.Configuration;
using MyHome.Constants;
using MyHome.Extensions;
using MyHome.Models;
using MyHome.Utilities;

using GT = Gadgeteer;
using System.IO;

namespace MyHome.Modules
{
#pragma warning disable 0612, 0618 // Ignore TempHumidity obsolete warning
    public sealed class WeatherManager : IWeatherManager
    {
        private readonly Logger _logger;
        private readonly LightSense _light;
        private readonly TempHumidity _sensor;
        private WeatherModel _weather;

        private SensorConfiguration _configuration;
        private IAwaitable _saveMeasurementThread = Awaitable.Default;

        public event WeatherManager.Measurement OnMeasurement;

        public delegate void Measurement(WeatherModel weather);

        public WeatherManager(TempHumidity tempHumidity, LightSense lightSense)
        {
            _logger = Logger.ForContext(this);
            _light = lightSense;
            _sensor = tempHumidity;
            _sensor.MeasurementComplete += Sensor_MeasurementComplete;
            _weather = new WeatherModel();
        }

        public double Luminosity
        {
            get { return _weather.Luminosity; }
        }

        public double Humidity
        {
            get { return _weather.Humidity; }
        }

        public double Temperature
        {
            get { return _weather.Temperature; }
        }

        public void Initialise(SensorConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void TakeMeasurement()
        {
            if (!_sensor.IsTakingMeasurements)
            {
                _logger.Information("Taking measurements from sensors");
                _sensor.RequestSingleMeasurement();
            }
        }

        private void Sensor_MeasurementComplete(TempHumidity sender, TempHumidity.MeasurementCompleteEventArgs e)
        {
            _logger.Information("Updated sensor readings");
            _weather = new WeatherModel(_light.GetIlluminance(), e.RelativeHumidity, e.Temperature);
            if (OnMeasurement != null)
            {
                OnMeasurement.Invoke(_weather);
            }
        }

        public void SaveMeasurementToSdCard(IFileManager _fileManager, WeatherModel weather, DateTime timestamp)
        {
            if (!_configuration.SaveMeasurementsToSdCard) return;
            if (_saveMeasurementThread.IsRunning) return;
            if (!_fileManager.HasFileSystem) return;

            _saveMeasurementThread = new Awaitable(() =>
            {
                var filename = string.Concat("measurements_", timestamp.Datestamp(), FileExtensions.Csv);
                var filepath = MyPath.Combine(Directories.Weather, filename);
                var fileExists = _fileManager.FileExists(filepath);

                using (var fs = _fileManager.GetFileStream(filepath, FileMode.Append, FileAccess.Write))
                {
                    if (!fileExists)
                    {
                        fs.WriteText("DateTime, Humidity, Luminosity, Temperature\r\n");
                    }

                    fs.WriteText(
                        "{0}, {1}, {2}, {3}\r\n",
                        timestamp.SortableDateTime(),
                        weather.Humidity,
                        weather.Luminosity,
                        weather.Temperature
                    );
                }

                _logger.Information("{0} updated", filename);
            });
        }
    }
#pragma warning restore 0612, 0618
}
