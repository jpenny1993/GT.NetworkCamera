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

        public DisplayManager(DisplayT35 lcd)
        {
            _logger = Logger.ForContext(this);
            _lcd = lcd;
            _window = lcd.WPFWindow;
            Initialise();
        }

        private void Initialise()
        {
            _window.Background = new SolidColorBrush(GT.Color.White);
            _window.Child = ScreenDashboard();
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
                        .AddChild(c => c.Button(b => b
                            .Text("Back")
                            .Background(GT.Color.LightGray)
                            .Foreground(GT.Color.Black)
                            .OnPressed(GT.Color.DarkGray, (sender, args) =>
                            {

                            }))));
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

        private UIElement PageHeader2
        {
            get
            {
                return GuiBuilder.Create()
                    .Panel(vp => vp
                        .Vertical()
                        .VerticalAlignTop()
                        .AddChild(c => c.Panel(hp => hp
                        .Horizontal()
                        .HorizontalAlignStretch()
                        .AddChild(hc => hc.Label(l => l
                            .Width(GuiBuilder.DeviceWidth)
                            .Background(GT.Color.Blue)
                            .Foreground(GT.Color.White)
                            .TextMarginLeft(GuiBuilder.DeviceWidth / 2)
                            .TextAlignLeft()
                            .Text(DateTime.Now.SortableDateTime()))))));
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
                                .Text("IP Address: 0.0.0.0"))))));
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

        private UIElement DashboardButton(Resources.BinaryResources icon)
        {
            // TODO make fluent,fix black borders, add click actions
            var vpanel = (StackPanel)GuiBuilder.Create().Panel(p => p.Vertical().VerticalAlignCenter().MarginTopBottom());
            var hpanel1 = (StackPanel)GuiBuilder.Create().Panel(p => p.Horizontal().HorizontalAlignCenter().MarginLeftRight());
            var hpanel2 = (StackPanel)GuiBuilder.Create().Panel(p => p.Horizontal().HorizontalAlignCenter().MarginLeftRight());
            var image = GuiBuilder.Create().Image(Resources.GetBytes(icon));
            var text = GuiBuilder.Create().Label(x => x.Text("Placeholder"));

            hpanel1.Children.Add(image);
            hpanel2.Children.Add(text);

            vpanel.Children.Add(hpanel1);
            vpanel.Children.Add(hpanel2);

            var border = new Border
            {
                Background = new SolidColorBrush(GT.Color.LightGray),
                Child = vpanel
            };

            border.SetBorderThickness(GuiBuilder.HalfMargin);
            border.SetMargin(GuiBuilder.Margin);

            return border;
        }

        private UIElement ScreenDashboard()
        {
            return GuiBuilder.Create()
                .Panel(root => root
                    .AddChild(PageHeader2)
                    .AddChild(main => main.Panel(vp => vp
                        .Vertical()
                        .VerticalAlignCenter()
                        .MarginLeftRight()
                        .AddChild(c => c.Panel(hp => hp
                            .Horizontal()
                            .HorizontalAlignCenter()
                            .MarginTopBottom()
                            .AddChild(DashboardButton(Resources.BinaryResources.Thermometer_Small))
                            .AddChild(DashboardButton(Resources.BinaryResources.Humidity_Small))
                        ))
                        .AddChild(c => c.Panel(hp => hp
                            .Horizontal()
                            .HorizontalAlignCenter()
                            .MarginTopBottom()
                            .AddChild(DashboardButton(Resources.BinaryResources.Sunshine_Small))
                            .AddChild(DashboardButton(Resources.BinaryResources.Humidity_Small))
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
                        .AddChild(PageBody(Resources.BinaryResources.Sunshine, "Light Level", "X Lux"))))
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
                        .AddChild(PageBody(Resources.BinaryResources.Humidity, "Humidity", "X g.m-3"))))
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
                        .AddChild(PageBody(Resources.BinaryResources.Thermometer, "Temperature", "X °C"))))
                    .AddChild(PageFooter));
        }
    }
}
