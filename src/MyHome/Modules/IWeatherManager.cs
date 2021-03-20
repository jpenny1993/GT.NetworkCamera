using System;

namespace MyHome.Modules
{
    public interface IWeatherManager
    {
        event WeatherManager.Measurement OnMeasurement;
        double Humidity { get; }
        double Temperature { get; }
        void Start();
        void Stop();
        void TakeMeasurement();
    }
}
