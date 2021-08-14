using System;
using System.Collections;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;
using MyHome.Models;

using GT = Gadgeteer;

namespace MyHome.Modules
{
#pragma warning disable 0612, 0618 // Ignore TempHumidity obsolete warning
    public sealed class WeatherManager : IWeatherManager
    {
        private const int TimerTickMs = 5000; // Every 5 seconds
        private readonly LightSense _light;
        private readonly TempHumidity _sensor;
        private readonly GT.Timer _updateTimer;
        private bool _started;
        private WeatherModel _weather;

        public event WeatherManager.Measurement OnMeasurement;

        public delegate void Measurement(WeatherModel weather);

        public WeatherManager(TempHumidity tempHumidity, LightSense lightSense)
        {
            _light = lightSense;
            _sensor = tempHumidity;
            _sensor.MeasurementComplete += Sensor_MeasurementComplete;
            _updateTimer = new GT.Timer(TimerTickMs);
            _updateTimer.Tick += UpdateTimer_Tick;
            _weather = new WeatherModel();
            _started = false;
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
            if (!_started)
            { 
                _sensor.StartTakingMeasurements();
                _updateTimer.Start();
                _started = true;
            }
        }

        public void Stop()
        {
            if (_started)
            { 
                _sensor.StopTakingMeasurements();
                _updateTimer.Stop();
                _started = false;
            }
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
        }

        private void UpdateTimer_Tick(GT.Timer timer)
        {
            if (OnMeasurement != null)
            {
                OnMeasurement.Invoke(_weather);
            }
        }
    }
#pragma warning restore 0612, 0618
}
