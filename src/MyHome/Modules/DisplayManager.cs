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
    public enum DisplayState
    {
        Startup = 0,
        Dashboard = 1,
        Notification = 2,
        Prompt = 3,
        PromptComplete = 4,
    }

    public class DisplayManager
    {
        private static readonly Brush WhiteBackgroundBrush = new SolidColorBrush(GT.Color.White);
        private static readonly TimeSpan OneSecond = TimeSpan.FromTicks(TimeSpan.TicksPerSecond);

        private readonly Logger _logger;
        private readonly DisplayT35 _lcd;
        private readonly Window _window;
        private readonly ISystemManager _system;

        private readonly TimeSpan _backlightTimeout;

        // Using uptime to avoid issues when time sync completes
        private TimeSpan _lastTouch;
        private TimeSpan _lastStateChange;

        private UIElement _dashboard;

        public DisplayState State { get; private set; }

        public bool IsDisplayActive { get { return _lcd.BacklightEnabled; } }

        public TimeSpan TimeSinceLastTouch { get { return _system.Uptime - _lastTouch; } }

        public bool IsReadyForScreenWake { get { return !IsDisplayActive && TimeSinceLastTouch < OneSecond; } }

        public bool IsReadyForScreenTimeout { get { return IsDisplayActive && State == DisplayState.Dashboard && TimeSinceLastTouch > _backlightTimeout; } }

        public DisplayManager(DisplayT35 lcd, ISystemManager system)
        {
            _logger = Logger.ForContext(this);
            _system = system;
            _backlightTimeout = new TimeSpan(0, 0, 30);
            _lcd = lcd;
            _window = lcd.WPFWindow;
            _window.TouchUp += Window_TouchUp;

            SetState(DisplayState.Startup);
            ShowStatusNotification("Loading...");
        }

        public void TouchScreen()
        {
            _lastTouch = _system.Uptime;
        }

        public void EnableBacklight()
        {
            _lcd.BacklightEnabled = true;
        }

        public void DismissBacklight()
        {
            _lcd.BacklightEnabled = false;
        }

        public void SwitchToDashboard()
        {
            SetState(DisplayState.Dashboard);
        }

        public void ShowStatusNotification(string status)
        {
             // Status notifications during startup should stay as startup notifications
            var nextState = State == DisplayState.Startup
                ? DisplayState.Startup
                : DisplayState.Notification;

            if (!CanChangeState(State, nextState)) return;

            var screen = GuiBuilder.Create()
                .Panel(vp => vp.Vertical().VerticalAlignCenter().AddChild(c1 => c1
                    .Panel(hp => hp.Horizontal().HorizontalAlignCenter().AddChild(c2 => c2
                        .Label(l => l.Text(status).Foreground(GT.Color.Black))
                    ))
                ));

            SetState(nextState);

            DispatchScreenUpdate(screen);
        }

        public void ShowDashboard(DateTime now, string ipAddress, double humidity, double luminosity, double temperature, double totalFreeSpaceInMb)
        {
            _dashboard = BuildDashboard(now, ipAddress, humidity, luminosity, temperature, totalFreeSpaceInMb);

            if (!CanChangeState(State, DisplayState.Dashboard)) return;

            SetState(DisplayState.Dashboard);

            DispatchScreenUpdate(_dashboard);
        }

        public void ShowAccessDenied()
        {
            if (!CanChangeState(State, DisplayState.PromptComplete)) return;

            var screen = GuiBuilder.Create()
                .Panel(vp => vp.Vertical().VerticalAlignCenter()
                    .AddChild(c1 => c1
                        .Panel(hp => hp.Horizontal().HorizontalAlignCenter().MarginTopBottom()
                            .AddChild(c2 => c2.Image(Resources.GetBytes(Resources.BinaryResources.Deny)))
                        )
                    )
                    .AddChild(c1 => c1
                        .Panel(hp => hp.Horizontal().HorizontalAlignCenter().MarginTopBottom()
                            .AddChild(c2 => c2.Label(l => l.Text("Access Denied").Foreground(GT.Color.Black)))
                        )
                    )
                );

            SetState(DisplayState.PromptComplete);

            DispatchScreenUpdate(screen);
        }

        public void ShowClockInOrOut(DateTime timestamp, string attendanceStatus, string displayName)
        {
            if (!CanChangeState(State, DisplayState.PromptComplete)) return;

            var screen = GuiBuilder.Create().Panel(c1 => c1.Vertical().VerticalAlignCenter().MarginLeftRight()
                .AddChild(c3 => c3.Panel(hp => hp.Horizontal().HorizontalAlignCenter().MarginTopBottom()
                    .AddChild(c4 => c4.Image(Resources.GetBytes(Resources.BinaryResources.Clock)))
                    .AddChild(c7 => c7.Panel(cvp => cvp.Vertical().VerticalAlignCenter().MarginLeftRight()
                        .AddChild(c8 => c8.Label(l1 => l1.Text(displayName).TextAlignLeft().TextMarginLeft()))
                        .AddChild(c8 => c8.Label(l1 => l1.Text(attendanceStatus).TextAlignLeft().TextMarginLeft()))
                        .AddChild(c8 => c8.Label(l1 => l1.Text(timestamp.TimeOfDay()).TextAlignLeft().TextMarginLeft()))
                    ))
                ))
                .AddChild(c3 => c3.Panel(hp => hp.Horizontal().HorizontalAlignCenter().MarginTopBottom()
                    .AddChild(c4 => c4.Label(l => l.Text("{0} COMPLETE!".Format(attendanceStatus)).Foreground(GT.Color.Black)))
                ))
            );

            SetState(DisplayState.PromptComplete);

            DispatchScreenUpdate(screen);
        }

        public void ShowClockInOrOutPrompt(DateTime timestamp, string status, string displayName, string question, TouchEventHandler acceptAction, TouchEventHandler denyAction)
        {
            if (!CanChangeState(State, DisplayState.Prompt)) return;

            var screen = GuiBuilder.Create().Panel(c1 => c1.Vertical().VerticalAlignCenter().MarginLeftRight()
                .AddChild(c3 => c3.Panel(hp => hp.Horizontal().HorizontalAlignCenter().MarginTopBottom()
                    .AddChild(c4 => c4.Image(Resources.GetBytes(Resources.BinaryResources.Clock)))
                    .AddChild(c7 => c7.Panel(cvp => cvp.Vertical().VerticalAlignCenter().MarginLeftRight()
                        .AddChild(c8 => c8.Label(l1 => l1.Text(displayName).TextAlignLeft().TextMarginLeft()))
                        .AddChild(c8 => c8.Label(l1 => l1.Text(status).TextAlignLeft().TextMarginLeft()))
                        .AddChild(c8 => c8.Label(l1 => l1.Text(timestamp.TimeOfDay()).TextAlignLeft().TextMarginLeft()))
                    ))
                ))
                .AddChild(c3 => c3.Panel(hp => hp.Horizontal().HorizontalAlignCenter().MarginTopBottom()
                    .AddChild(c4 => c4.Label(l => l.Text(question).Foreground(GT.Color.Black)))
                ))
                .AddChild(c3 => c3.Panel(hp => hp.Horizontal().HorizontalAlignCenter().MarginTopBottom()
                    .AddChild(c4 => c4.Button(b => b
                        .Content(c5 => c5.Image(Resources.GetBytes(Resources.BinaryResources.Accept)))
                        .Background(GT.Color.White)
                        .MarginRight()
                        .OnPressed(GT.Color.Blue, acceptAction)
                    ))
                    .AddChild(c4 => c4.Button(b => b
                        .Content(c5 => c5.Image(Resources.GetBytes(Resources.BinaryResources.Deny)))
                        .Background(GT.Color.White)
                        .MarginRight()
                        .OnPressed(GT.Color.Blue, denyAction)
                    ))
                ))
            );

            SetState(DisplayState.Prompt);

            DispatchScreenUpdate(screen);
        }

        public void RefreshState(TimeSpan uptime)
        {
            var timeSinceStart = uptime - _lastStateChange;
            var fiveSeconds = TimeSpan.FromTicks(TimeSpan.TicksPerSecond * 5);
            if (timeSinceStart < fiveSeconds) return;

            if (CanChangeState(State, DisplayState.Dashboard))
            {
                ReturnToDashboard();
            }
        }

        public void ReturnToDashboard()
        {
            SetState(DisplayState.Dashboard);
            if (_dashboard != null)
                DispatchScreenUpdate(_dashboard);
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

        private void DispatchScreenUpdate(UIElement screen)
        {
            DispatchScreenUpdate(WhiteBackgroundBrush, screen);
        }

        private void DispatchScreenUpdate(Brush background, UIElement screen)
        {
            _window.Dispatcher.BeginInvoke((object obj) =>
            {
                if (_window.Background != background)
                {
                    _window.Background = background;
                }

                _window.Child = screen;
                return null;
            }, null);
        }

        private void SetState(DisplayState value)
        {
            State = value;
            _lastStateChange = _system.Uptime;
        }

        private void Window_TouchUp(object sender, TouchEventArgs e)
        {
            TouchScreen();
        }

        private static bool CanChangeState(DisplayState oldState, DisplayState newState)
        {
            switch (oldState)
            {
                default:                                    return false;
                case DisplayState.Startup:
                    switch (newState)
                    {
                        default:                            return false;
                        case DisplayState.Startup:          return true;
                        case DisplayState.Dashboard:        return true;
                        case DisplayState.Notification:     return false;
                        case DisplayState.Prompt:           return false;
                        case DisplayState.PromptComplete:   return false;
                    }
                case DisplayState.Dashboard:
                    switch (newState)
                    {
                        default:                            return false;
                        case DisplayState.Startup:          return false;
                        case DisplayState.Dashboard:        return true;
                        case DisplayState.Notification:     return true;
                        case DisplayState.Prompt:           return true;
                        case DisplayState.PromptComplete:   return true;
                    }
                case DisplayState.Notification:
                    switch (newState)
                    {
                        default:                            return false;
                        case DisplayState.Startup:          return false;
                        case DisplayState.Dashboard:        return true;
                        case DisplayState.Notification:     return false;
                        case DisplayState.Prompt:           return false;
                        case DisplayState.PromptComplete:   return false;
                    }
                case DisplayState.Prompt:
                    switch (newState)
                    {
                        default:                            return false;
                        case DisplayState.Startup:          return false;
                        case DisplayState.Dashboard:        return false;
                        case DisplayState.Notification:     return false;
                        case DisplayState.Prompt:           return false;
                        case DisplayState.PromptComplete:   return true;
                    }
                case DisplayState.PromptComplete:
                    switch (newState)
                    {
                        default:                            return false;
                        case DisplayState.Startup:          return false;
                        case DisplayState.Dashboard:        return true;
                        case DisplayState.Notification:     return false;
                        case DisplayState.Prompt:           return false;
                        case DisplayState.PromptComplete:   return false;
                    }
            }
        }
    }
}
