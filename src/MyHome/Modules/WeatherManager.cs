using System;
using System.Collections;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;
using MyHome.Models;

namespace MyHome.Modules
{
#pragma warning disable 0612, 0618 // Ignore TempHumidity obsolete warning
    public sealed class WeatherManager : IWeatherManager
    {
        private const int TimerTickMs = 5000; // Every 5 seconds
        private readonly LightSense _light;
        private readonly TempHumidity _sensor;
        private WeatherModel _weather;

        public event WeatherManager.Measurement OnMeasurement;

        public delegate void Measurement(WeatherModel weather);

        public WeatherManager(TempHumidity tempHumidity, LightSense lightSense)
        {
            _light = lightSense;
            _sensor = tempHumidity;
            _sensor.MeasurementInterval = TimerTickMs;
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

        public void Start()
        {
            _sensor.StartTakingMeasurements();
        }

        public void Stop()
        {
            _sensor.StopTakingMeasurements();
        }

        public void TakeMeasurement()
        {
            if (!_sensor.IsTakingMeasurements)
            { 
                _sensor.RequestSingleMeasurement();
            }
        }

        private void Sensor_MeasurementComplete(TempHumidity sender, TempHumidity.MeasurementCompleteEventArgs e)
        {
            _weather = new WeatherModel(_light.GetIlluminance(), e.RelativeHumidity, e.Temperature);

            if (OnMeasurement != null)
            {
                OnMeasurement.Invoke(_weather);
            }
        }
    }
#pragma warning restore 0612, 0618
}
