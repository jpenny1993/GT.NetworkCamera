using System;
using Microsoft.SPOT;

namespace MyHome.Models
{
    public class WeatherModel
    {
        public double Humidity { get; }

        public double Luminosity { get; }

        public double Temperature { get; }

        public WeatherModel() { }

        public WeatherModel(double luminosity, double humidity, double temperature)
        {
            Luminosity = luminosity;
            Humidity = humidity;
            Temperature = temperature;
        }
    }
}
