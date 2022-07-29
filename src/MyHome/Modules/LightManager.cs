using System;
using Microsoft.SPOT;
using Gadgeteer;
using Gadgeteer.Modules.GHIElectronics;
using MyHome.Configuration;
using MyHome.Extensions;

namespace MyHome.Modules
{
    public sealed class LightManager
    {
        private static bool _enabled;

        public sealed class Light
        {
            private readonly MulticolorLED _led;

            private Color _colour;
            private bool _blink;

            public Light(MulticolorLED led)
            {
                _led = led;
            }

            public static void Enable(Light light)
            {
                if (light._colour == Color.Black) return;

                if (light._blink)
                    light.BlinkColour(light._colour);
                else
                    light.SetColour(light._colour);
            }

            public static void Disable(Light light)
            {
                light._led.TurnOff();
            }

            public void TurnOff()
            {
                _blink = false;
                _colour = Color.Black;
                _led.TurnOff();
            }

            public void TurnBlue()     { SetColour(Color.Blue); }

            public void TurnCyan()     { SetColour(Color.Cyan); }

            public void TurnGreen()    { SetColour(Color.Green); }

            public void TurnMagenta()  { SetColour(Color.Magenta); }

            public void TurnOrange()   { SetColour(Color.Orange); }

            public void TurnRed()      { SetColour(Color.Red); }

            public void TurnYellow()   { SetColour(Color.Yellow); }

            public void BlinkBlue()    { BlinkColour(Color.Blue); }

            public void BlinkCyan()    { BlinkColour(Color.Cyan); }

            public void BlinkGreen()   { BlinkColour(Color.Green); }

            public void BlinkMagenta() { BlinkColour(Color.Magenta); }

            public void BlinkOrange()  { BlinkColour(Color.Orange); }

            public void BlinkRed()     { BlinkColour(Color.Red); }

            public void BlinkYellow()  { BlinkColour(Color.Yellow); }
            
            public void WinkBlue()     { WinkColour(Color.Blue); }
                                       
            public void WinkCyan()     { WinkColour(Color.Cyan); }
                                       
            public void WinkGreen()    { WinkColour(Color.Green); }
                                       
            public void WinkMagenta()  { WinkColour(Color.Magenta); }
                                       
            public void WinkOrange()   { WinkColour(Color.Orange); }
                                       
            public void WinkRed()      { WinkColour(Color.Red); }
                                       
            public void WinkYellow()   { WinkColour(Color.Yellow); }

            private void SetColour(Color colour)
            {
                _colour = colour;
                _blink = false;
                if (_enabled)
                {
                    _led.TurnColor(colour);
                }
            }

            private void BlinkColour(Color colour)
            {
                _colour = colour;
                _blink = true;
                if (_enabled)
                {
                    _led.BlinkRepeatedly(colour);
                }
            }

            private void WinkColour(Color colour)
            {
                if (_enabled)
                {
                    _led.BlinkOnce(colour, new TimeSpan(0, 0, 3), _colour);
                }
            }
        }

        private LightConfiguration _configuration;

        public LightManager(MulticolorLED networkLED, MulticolorLED infoLED)
        {
            NetworkLED = new Light(networkLED);
            InfoLED = new Light(infoLED);
            _enabled = true;
        }

        public Light NetworkLED { get; private set; }

        public Light InfoLED { get; private set; }

        public void Initialise(LightConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void DisableAll()
        {
            if (_enabled)
            {
                _enabled = false;
                Light.Disable(NetworkLED);
                Light.Disable(InfoLED);
            }
        }

        public void EnableAll()
        {
            if (!_enabled)
            {
                _enabled = true;
                Light.Enable(NetworkLED);
                Light.Enable(InfoLED);
            }
        }

        public void CheckScheduleForLightsOut(DateTime timestamp)
        {
            if (timestamp.IsInRange(_configuration.LEDsOnFrom, _configuration.LEDsOnUntil))
            {
                EnableAll();
            }
            else 
            {
                DisableAll();
            }
        }
    }
}
