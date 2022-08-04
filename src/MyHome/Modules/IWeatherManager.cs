using System;

namespace MyHome.Modules
{
    public interface IWeatherManager
    {
        event WeatherManager.Measurement OnMeasurement;
        double Luminosity { get; }
        double Humidity { get; }
        double Temperature { get; }
        void TakeMeasurement();
    }
}
