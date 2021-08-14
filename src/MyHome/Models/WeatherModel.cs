using System;
using Microsoft.SPOT;

namespace MyHome.Models
{
    public class WeatherModel
    {
        public double Humidity { get; private set; }

        public double Luminosity { get; private set; }

        public double Temperature { get; private set; }

        public WeatherModel() { }

        public WeatherModel(double luminosity, double humidity, double temperature)
        {
            Luminosity = luminosity;
            Humidity = humidity;
            Temperature = temperature;
        }
    }
}
