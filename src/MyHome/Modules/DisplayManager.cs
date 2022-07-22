using System;
using Gadgeteer.Modules.GHIElectronics;
using Microsoft.SPOT;
using Microsoft.SPOT.Input;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using MyHome.Extensions;
using MyHome.Utilities;

using GT = Gadgeteer;

namespace MyHome.Modules
{
    public class DisplayManager
    {
        private readonly Logger _logger;
        private readonly DisplayT35 _lcd;
        private readonly Window _window;

        private TimeSpan _backlightTimeout = new TimeSpan(0, 0, 30);
        private DateTime _lastTouch;

        public DisplayManager(DisplayT35 lcd)
        {
            _logger = Logger.ForContext(this);
            _lcd = lcd;
            _window = lcd.WPFWindow;
            _window.TouchUp += Window_TouchUp;

            var loadingScreen = GuiBuilder.Create()
                .Panel(vp => vp.Vertical().VerticalAlignCenter().AddChild(c1 => c1
                    .Panel(hp => hp.Horizontal().HorizontalAlignCenter().AddChild(c2 => c2
                        .Label(l => l.Text("Loading..."))
                    ))
                ));

            _window.Dispatcher.BeginInvoke((object obj) =>
            {
                _window.Background = new SolidColorBrush(GT.Color.White);
                _window.Child = loadingScreen;
                return null;
            }, null);
        }

        public void TouchScreen()
        {
            _lastTouch = DateTime.Now;
            if (!_lcd.BacklightEnabled)
            {
                _lcd.BacklightEnabled = true;
            }
        }

        public void DismissBacklight()
        {
            var diff = DateTime.Now - _lastTouch;
            if (diff > _backlightTimeout)
            {
                _lcd.BacklightEnabled = false;
            }
        }

        public void ShowDashboard(DateTime now, string ipAddress, double humidity, double luminosity, double temperature, double totalFreeSpaceInMb)
        {
            var screen = BuildDashboard(now, ipAddress, humidity, luminosity, temperature, totalFreeSpaceInMb);
            _window.Dispatcher.BeginInvoke((object obj) =>
            {
                _window.Child = screen;
                return null;
            }, null);
        }

        private UIElement BuildDashboard(DateTime now, string ipAddress, double humidity, double luminosity, double temperature, double totalFreeSpaceInMb)
        {
            return GuiBuilder.Create().Panel(root => root
                // Page header
                .AddChild(c1 => c1.Panel(vp => vp.Vertical().VerticalAlignTop().AddChild(c2 => c2
                    .Panel(hp => hp.Horizontal().HorizontalAlignStretch().AddChild(c3 => c3
                        .Label(l => l
                            .Text(now.SortableDateTime(includeSeconds: false))
                            .Width(GuiBuilder.DeviceWidth)
                            .Background(GT.Color.Blue)
                            .Foreground(GT.Color.White)
                            .TextMarginRight(GuiBuilder.Margin * 3)
                            .TextAlignRight()
                        ))
                    ))
                ))
                // Page body
                .AddChild(c1 => c1.Panel(vp => vp.Vertical().VerticalAlignCenter()
                    // Humidity widget
                    .AddChild(c2 => c2.Panel(hp => hp.Horizontal().HorizontalAlignCenter().MarginTopBottom()
                        .AddChild(c3 => c3.Image(Resources.GetBytes(Resources.BinaryResources.Humidity_Small)))
                        .AddChild(c4 => c4.Panel(vp2 => vp2.Vertical().VerticalAlignCenter().MarginLeftRight()
                            .AddChild(c5 => c5.Label(l => l.Text("Humidity").TextAlignLeft().TextMarginLeft()))
                            .AddChild(c5 => c5.Label(l => l.Text("{0} g.m-3".Format(humidity.ToString("N2"))).TextAlignLeft().TextMarginLeft()))
                        ))
                    ))
                    // Illuminance widget
                    .AddChild(c2 => c2.Panel(hp => hp.Horizontal().HorizontalAlignCenter().MarginTopBottom()
                        .AddChild(c3 => c3.Image(Resources.GetBytes(Resources.BinaryResources.Sunshine_Small)))
                        .AddChild(c4 => c4.Panel(vp2 => vp2.Vertical().VerticalAlignCenter().MarginLeftRight()
                            .AddChild(c5 => c5.Label(l => l.Text("Illuminance").TextAlignLeft().TextMarginLeft()))
                            .AddChild(c5 => c5.Label(l => l.Text("{0} Lux".Format(luminosity.ToString("N2"))).TextAlignLeft().TextMarginLeft()))
                        ))
                    ))
                    // Temperature widget
                    .AddChild(c2 => c2.Panel(hp => hp.Horizontal().HorizontalAlignCenter().MarginTopBottom()
                        .AddChild(c3 => c3.Image(Resources.GetBytes(Resources.BinaryResources.Thermometer_Small)))
                        .AddChild(c4 => c4.Panel(vp2 => vp2.Vertical().VerticalAlignCenter().MarginLeftRight()
                            .AddChild(c5 => c5.Label(l => l.Text("Temperature").TextAlignLeft().TextMarginLeft()))
                            .AddChild(c5 => c5.Label(l => l.Text("{0} °C".Format(temperature.ToString("N2"))).TextAlignLeft().TextMarginLeft()))
                        ))
                    ))
                ))
                // Page footer
                .AddChild(c1 => c1.Panel(vp => vp.Vertical().VerticalAlignBottom().AddChild(c2 => c2
                    .Panel(hp => hp.Horizontal().HorizontalAlignStretch().AddChild(c3 => c3
                        .Label(l => l
                            .Text(ipAddress)
                            .Width(GuiBuilder.DeviceWidth / 2)
                            .Background(GT.Color.Gray)
                            .Foreground(GT.Color.White)
                            .TextMarginLeft()
                            .TextAlignLeft()
                        )).AddChild(c4 => c4.Label(l => l
                            .Text("{0} MB Free".Format(totalFreeSpaceInMb.ToString("N2")))
                            .Width(GuiBuilder.DeviceWidth / 2)
                            .Background(GT.Color.Gray)
                            .Foreground(GT.Color.White)
                            .TextMarginRight(GuiBuilder.Margin * 3)
                            .TextAlignRight()
                        ))
                    ))
                ))
            );
        }

        //private UIElement BackButtonPanel
        //{
        //    get
        //    {
        //        return GuiBuilder.Create()
        //            .Panel(hp => hp
        //                .Horizontal()
        //                .HorizontalAlignRight()
        //                .MarginAll()
        //                .AddChild(hc => hc.Button(b => b
        //                    .Content("Back")
        //                    .Background(GT.Color.LightGray)
        //                    .Foreground(GT.Color.Black)
        //                    .OnPressed(GT.Color.DarkGray, NavigationEvent(ScreenDashboard)))));
        //    }
        //}

        //private UIElement DashboardButton(Resources.BinaryResources icon, string text, TouchEventHandler onPressed)
        //{
        //    return GuiBuilder.Create()
        //        .Button(b => b
        //            .Background(GT.Color.White)
        //            .MarginAll()
        //            .OnPressed(GT.Color.DarkGray, onPressed)
        //            .Content(bc => bc.Panel(vp => vp
        //                .Vertical()
        //                .VerticalAlignCenter()
        //                .MarginTopBottom()
        //                .AddChild(vc => vc.Panel(hp => hp
        //                    .Horizontal()
        //                    .HorizontalAlignCenter()
        //                    .MarginLeftRight()
        //                    .AddChild(hc => hc.Image(Resources.GetBytes(icon)))))
        //                .AddChild(vc => vc.Panel(hp => hp
        //                    .Horizontal()
        //                    .HorizontalAlignCenter()
        //                    .MarginLeftRight()
        //                    .AddChild(hc => hc.Label(l => l.Text(text))))))));
        //}

        //private TouchEventHandler NavigationEvent(GuiBuilder.UiElementEvent pageBuilder)
        //{
        //    return new TouchEventHandler((sender, args) => _window.Child = pageBuilder.Invoke());
        //}

        private void Window_TouchUp(object sender, TouchEventArgs e)
        {
            if (!_lcd.BacklightEnabled)
            {
                _lcd.BacklightEnabled = true;
            }

            _lastTouch = DateTime.Now;
        }
    }
}
