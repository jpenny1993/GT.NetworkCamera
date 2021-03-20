using System;
using System.Collections;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;

namespace MyHome.Modules
{
#pragma warning disable 0612, 0618 // Ignore TempHumidity obsolete warning
    public sealed class WeatherManager : IWeatherManager
    {
        private readonly LightSense _light;
        private readonly TempHumidity _sensor;
        private double _humidity;
        private double _temperature;

        public event WeatherManager.Measurement OnMeasurement;

        public delegate void Measurement(double humidity, double temperature);

        public WeatherManager(TempHumidity tempHumidity, LightSense lightSense)
        {
            _light = lightSense;
            _sensor = tempHumidity;
            _sensor.MeasurementComplete += Sensor_MeasurementComplete;
        }

        public double Luminosity
        {
            get { return _light.GetIlluminance(); }
        }

        public double Humidity
        {
            get { return _humidity; }
        }

        public double Temperature
        {
            get { return _temperature; }
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
            _humidity = e.RelativeHumidity;
            _temperature = e.Temperature;

            if (OnMeasurement != null)
            {
                OnMeasurement.Invoke(_humidity, _temperature);
            }
        }
    }
#pragma warning restore 0612, 0618
}
