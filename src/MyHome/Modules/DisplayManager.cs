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
        private const int TimerTickMs = 5000;
        private const string Humidity = "Humidity";
        private const string LightLevel = "Light Level";
        private const string Temperature = "Temperature";

        private readonly Logger _logger;
        private readonly DisplayT35 _lcd;
        private readonly Window _window;
        private readonly GT.Timer _timer;

        private readonly INetworkManager _networkManager;
        private readonly IWeatherManager _weatherManager;

        private TimeSpan _backlightTimeout = new TimeSpan(0, 0, 10);
        private DateTime _lastTouch;

        public DisplayManager(DisplayT35 lcd,
            INetworkManager networkManager,
            IWeatherManager weatherManager)
        {
            _logger = Logger.ForContext(this);
            _lcd = lcd;
            _window = lcd.WPFWindow;
            _networkManager = networkManager;
            _weatherManager = weatherManager;

            _window.TouchUp += Window_TouchUp;
            _window.Dispatcher.BeginInvoke((object obj) =>
            {
                _window.Background = new SolidColorBrush(GT.Color.White);
                _window.Child = ScreenDashboard();
                return null;
            }, null);

            _lastTouch = DateTime.Now;
            _timer = new GT.Timer(TimerTickMs);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private UIElement BackButtonPanel
        {
            get
            {
                return GuiBuilder.Create()
                    .Panel(hp => hp
                        .Horizontal()
                        .HorizontalAlignRight()
                        .MarginAll()
                        .AddChild(hc => hc.Button(b => b
                            .Content("Back")
                            .Background(GT.Color.LightGray)
                            .Foreground(GT.Color.Black)
                            .OnPressed(GT.Color.DarkGray, NavigationEvent(ScreenDashboard)))));
            }
        }

        private UIElement PageHeader
        {
            get
            {
                return GuiBuilder.Create()
                    .Panel(hp => hp
                        .Horizontal()
                        .HorizontalAlignStretch()
                        .AddChild(hc => hc.Label(l => l
                            .Width(GuiBuilder.DeviceWidth)
                            .Background(GT.Color.Blue)
                            .Foreground(GT.Color.White)
                            .TextMarginLeft(GuiBuilder.DeviceWidth / 2)
                            .TextAlignLeft()
                            .Text(DateTime.Now.SortableDateTime()))));
            }
        }

        private UIElement PageFooter
        {
            get
            {
                return GuiBuilder.Create()
                    .Panel(vp => vp
                        .Vertical()
                        .VerticalAlignBottom()
                        .AddChild(c => c.Panel(hp => hp
                            .Horizontal()
                            .HorizontalAlignStretch()
                            .AddChild(x => x.Label(l => l
                                .Width(GuiBuilder.DeviceWidth)
                                .Background(GT.Color.Gray)
                                .Foreground(GT.Color.White)
                                .TextMarginLeft()
                                .TextAlignLeft()
                                .Text("IP Address: {0}".Format(_networkManager.IpAddress)))))));
            }
        }

        private UIElement PageBody(Resources.BinaryResources icon, string title, string value)
        {
            return GuiBuilder.Create()
                .Panel(hp => hp
                    .Horizontal()
                    .HorizontalAlignCenter()
                    .MarginTopBottom()
                    .AddChild(x => x.Image(Resources.GetBytes(icon)))
                    .AddChild(hc => hc.Panel(vp => vp
                        .Vertical()
                        .VerticalAlignCenter()
                        .MarginLeftRight()
                        .AddChild(x => x.Label(l => l.Text(title)))
                        .AddChild(x => x.Label(l => l.Text(value))))));
        }

        private UIElement DashboardButton(Resources.BinaryResources icon, string text, TouchEventHandler onPressed)
        {
            return GuiBuilder.Create()
                .Button(b => b
                    .Background(GT.Color.White)
                    .MarginAll()
                    .OnPressed(GT.Color.DarkGray, onPressed)
                    .Content(bc => bc.Panel(vp => vp
                        .Vertical()
                        .VerticalAlignCenter()
                        .MarginTopBottom()
                        .AddChild(vc => vc.Panel(hp => hp
                            .Horizontal()
                            .HorizontalAlignCenter()
                            .MarginLeftRight()
                            .AddChild(hc => hc.Image(Resources.GetBytes(icon)))))
                        .AddChild(vc => vc.Panel(hp => hp
                            .Horizontal()
                            .HorizontalAlignCenter()
                            .MarginLeftRight()
                            .AddChild(hc => hc.Label(l => l.Text(text))))))));
        }

        private TouchEventHandler NavigationEvent(GuiBuilder.UiElementEvent pageBuilder)
        {
            return new TouchEventHandler((sender, args) => _window.Child = pageBuilder.Invoke());
        }

        private UIElement ScreenDashboard()
        {
            return GuiBuilder.Create()
                .Panel(root => root
                    .AddChild(c => c.Panel(vp => vp
                        .Vertical()
                        .VerticalAlignTop()
                        .AddChild(PageHeader)))
                    .AddChild(main => main.Panel(vp => vp
                        .Vertical()
                        .VerticalAlignCenter()
                        .MarginLeftRight()
                        .AddChild(c => c.Panel(hp => hp
                            .Horizontal()
                            .HorizontalAlignCenter()
                            .MarginTopBottom()
                            .AddChild(DashboardButton(Resources.BinaryResources.Thermometer_Small, Temperature, NavigationEvent(ScreenTemperature)))
                            .AddChild(DashboardButton(Resources.BinaryResources.Humidity_Small, Humidity, NavigationEvent(ScreenHumidity)))
                        ))
                        .AddChild(c => c.Panel(hp => hp
                            .Horizontal()
                            .HorizontalAlignCenter()
                            .MarginTopBottom()
                            .AddChild(DashboardButton(Resources.BinaryResources.Sunshine_Small, LightLevel, NavigationEvent(ScreenLight)))
                            //.AddChild(DashboardButton(Resources.BinaryResources.Padlock_Small, "Lock Device", NavigationEvent(ScreenLock)))
                        ))))
                    .AddChild(PageFooter));
        }

        private UIElement ScreenLight()
        {
            return GuiBuilder.Create()
                .Panel(root => root
                    .AddChild(main => main.Panel(vp => vp
                        .Vertical()
                        .VerticalAlignTop()
                        .AddChild(PageHeader)
                        .AddChild(BackButtonPanel)
                        .AddChild(PageBody(Resources.BinaryResources.Sunshine, LightLevel, "{0} Lux".Format(_weatherManager.Luminosity)))))
                    .AddChild(PageFooter));
        }

        private UIElement ScreenHumidity()
        {
            return GuiBuilder.Create()
                .Panel(root => root
                    .AddChild(main => main.Panel(vp => vp
                        .Vertical()
                        .VerticalAlignTop()
                        .AddChild(PageHeader)
                        .AddChild(BackButtonPanel)
                        .AddChild(PageBody(Resources.BinaryResources.Humidity, Humidity, "{0} g.m-3".Format(_weatherManager.Humidity)))))
                    .AddChild(PageFooter));
        }

        private UIElement ScreenTemperature()
        {
            return GuiBuilder.Create()
                .Panel(root => root
                    .AddChild(main => main.Panel(vp => vp
                        .Vertical()
                        .VerticalAlignTop()
                        .AddChild(PageHeader)
                        .AddChild(BackButtonPanel)
                        .AddChild(PageBody(Resources.BinaryResources.Thermometer, Temperature, "{0} °C".Format(_weatherManager.Temperature)))))
                    .AddChild(PageFooter));
        }

        private void Timer_Tick(GT.Timer timer)
        {
            var diff = DateTime.Now - _lastTouch;
            if (diff > _backlightTimeout)
            {
                _lcd.BacklightEnabled = false;
                _timer.Stop();
            }
        }

        private void Window_TouchUp(object sender, TouchEventArgs e)
        {
            if (!_lcd.BacklightEnabled)
            {
                _window.Child = ScreenDashboard();
                _lcd.BacklightEnabled = true;
                _timer.Start();
            }

            _lastTouch = DateTime.Now;
        }
    }
}
