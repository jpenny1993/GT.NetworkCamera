
using System;
using Gadgeteer.Modules.GHIElectronics;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;

using GT = Gadgeteer;
using Microsoft.SPOT.Input;
using System.Collections;

namespace MyHome.Utilities
{
    public class PanelBuilder
    {
        private bool _stackPanel;
        private int _bm, _lm, _rm, _tm;
        private Orientation _orientation = Orientation.Horizontal;
        private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Stretch;
        private VerticalAlignment _verticalAlignment = VerticalAlignment.Stretch;
        private ArrayList _children = new ArrayList();

        public PanelBuilder AddChild(GuiBuilder.GuiBuilderEvent childBuilder)
        {
            var builder = GuiBuilder.Create();
            var child = childBuilder.Invoke(builder);
            _children.Add(child);
            return this;
        }

        public PanelBuilder AddChild(UIElement element)
        {
            _children.Add(element);
            return this;
        }

        public PanelBuilder Horizontal() 
        {
            _orientation = Orientation.Horizontal;
            _stackPanel = true;
            return this;
        }

        public PanelBuilder HorizontalAlignCenter()
        {
            _horizontalAlignment = HorizontalAlignment.Center;
            return this;
        }

        public PanelBuilder HorizontalAlignLeft()
        {
            _horizontalAlignment = HorizontalAlignment.Left;
            return this;
        }

        public PanelBuilder HorizontalAlignRight()
        {
            _horizontalAlignment = HorizontalAlignment.Right;
            return this;
        }

        public PanelBuilder HorizontalAlignStretch()
        {
            _horizontalAlignment = HorizontalAlignment.Stretch;
            return this;
        }

        public PanelBuilder MarginAll()
        {
            _lm = GuiBuilder.HalfMargin;
            _tm = GuiBuilder.HalfMargin;
            _rm = GuiBuilder.HalfMargin;
            _bm = GuiBuilder.HalfMargin;
            return this;
        }

        public PanelBuilder MarginLeftRight()
        {
            _lm = GuiBuilder.HalfMargin;
            _tm = 0;
            _rm = GuiBuilder.HalfMargin;
            _bm = 0;
            return this;
        }

        public PanelBuilder MarginTopBottom()
        {
            _lm = 0;
            _tm = GuiBuilder.HalfMargin;
            _rm = 0;
            _bm = GuiBuilder.HalfMargin;
            return this;
        }

        public PanelBuilder Vertical() 
        {
            _orientation = Orientation.Vertical;
            _stackPanel = true;
            return this;
        }

        public PanelBuilder VerticalAlignBottom() 
        {
            _verticalAlignment = VerticalAlignment.Bottom;
            return this;
        }

        public PanelBuilder VerticalAlignCenter()
        {
            _verticalAlignment = VerticalAlignment.Center;
            return this;
        }

        public PanelBuilder VerticalAlignStretch()
        {
            _verticalAlignment = VerticalAlignment.Stretch;
            return this;
        }

        public PanelBuilder VerticalAlignTop()
        {
            _verticalAlignment = VerticalAlignment.Top;
            return this;
        }

        private Panel Build()
        {
            var panel = !_stackPanel 
                ? new Panel() 
                : new StackPanel
                {
                    Orientation = _orientation,
                    HorizontalAlignment = _horizontalAlignment,
                    VerticalAlignment = _verticalAlignment
                };

            panel.SetMargin(_lm, _tm, _rm, _bm);

            foreach (var child in _children)
            {
                panel.Children.Add((UIElement)child);
            }

            return panel;
        }

        public static Panel Build(PanelBuilder builder) 
        {
            return builder.Build();
        }
    }

    public class ButtonBuilder
    {
        private int _bm, _lm, _rm, _tm;
        private GT.Color _backgroundColour = GT.Color.Gray;
        private GT.Color _foregroundColour = GT.Color.Black;
        private GT.Color _pressedColour = GT.Color.DarkGray;
        private TouchEventHandler _onPressedHandler;
        private string _text = string.Empty;
        private UIElement _child;

        public ButtonBuilder Background(GT.Color colour)
        {
            _backgroundColour = colour;
            return this;
        }

        public ButtonBuilder Content(string text)
        {
            _text = text;
            return this;
        }

        public ButtonBuilder Content(GuiBuilder.GuiBuilderEvent childBuilder)
        {
            var builder = GuiBuilder.Create();
            _child = childBuilder.Invoke(builder);
            return this;
        }

        public ButtonBuilder Foreground(GT.Color colour)
        {
            _foregroundColour = colour;
            return this;
        }

        public ButtonBuilder MarginAll()
        {
            _bm = _lm = _rm = _tm = GuiBuilder.Margin;
            return this;
        }

        public ButtonBuilder MarginRight()
        {
            _rm = GuiBuilder.Margin;
            return this;
        }

        public ButtonBuilder OnPressed(GT.Color colour, TouchEventHandler handler)
        {
            _pressedColour = colour;
            _onPressedHandler = handler;
            return this;
        }

        private UIElement Build()
        {
            var button = new Border()
            {
                BorderBrush = new SolidColorBrush(_backgroundColour)
            };

            button.Child = _child != null ? _child
                : new Text(GuiBuilder.Font, _text)
                {
                    ForeColor = _foregroundColour,
                    TextAlignment = TextAlignment.Center,
                    TextWrap = true
                };

            button.SetBorderThickness(5);

            button.TouchDown += (sender, args) =>
            {
                var border = (Border)sender;
                border.Dispatcher.BeginInvoke((obj) =>
                {
                    border.BorderBrush = new SolidColorBrush(_pressedColour);
                    return null;
                }, null);
            };

            if (_onPressedHandler != null)
            {
                button.TouchUp += (sender, args) => 
                {
                    var border = (Border)sender;
                    border.Dispatcher.BeginInvoke((obj) => {
                        border.BorderBrush = new SolidColorBrush(_backgroundColour);
                        _onPressedHandler.Invoke(sender, args);
                        return null;
                    }, null);
                };
            }

            if (_rm > 0)
            {
                button.SetMargin(_lm, _tm, _rm, _bm);
            }

            return button;
        }

        public static UIElement Build(ButtonBuilder builder)
        {
            return builder.Build();
        }
    }

    public class LabelBuilder
    {
        private int _bm, _lm, _rm, _tm;
        private int _width;
        private bool _hasBackgroundColour = false;
        private GT.Color _backgroundColour;
        private GT.Color _foregroundColour = GT.Color.Black;
        private string _text = string.Empty;
        private TextAlignment _textAlignment = TextAlignment.Center;

        public LabelBuilder Background(GT.Color colour)
        {
            _hasBackgroundColour = true;
            _backgroundColour = colour;
            return this;
        }

        public LabelBuilder Foreground(GT.Color colour)
        {
            _foregroundColour = colour;
            return this;
        }

        public LabelBuilder TextMarginBottom(int value = GuiBuilder.Margin)
        {
            _bm = value;
            return this;
        }

        public LabelBuilder TextMarginLeft(int value = GuiBuilder.Margin)
        {
            _lm = value;
            return this;
        }

        public LabelBuilder TextMarginRight(int value = GuiBuilder.Margin)
        {
            _rm = value;
            return this;
        }

        public LabelBuilder TextMarginTop(int value = GuiBuilder.Margin)
        {
            _tm = value;
            return this;
        }

        public LabelBuilder Text(string text)
        {
            _text = text;
            return this;
        }

        public LabelBuilder TextAlignCenter()
        {
            _textAlignment = TextAlignment.Center;
            return this;
        }

        public LabelBuilder TextAlignLeft()
        {
            _textAlignment = TextAlignment.Left;
            return this;
        }

        public LabelBuilder TextAlignRight()
        {
            _textAlignment = TextAlignment.Right;
            return this;
        }

        public LabelBuilder Width(int width)
        {
            _width = width;
            return this;
        }

        private UIElement Build()
        {
            UIElement element;

            var label = new Text(GuiBuilder.Font, _text)
            {
                ForeColor = _foregroundColour,
                TextAlignment = _textAlignment,
                TextWrap = true
            };

            label.SetMargin(_lm, _tm, _rm, _bm);

            if (!_hasBackgroundColour)
            {
                element = label;
            }
            else 
            {
                element = new Border
                {
                    BorderBrush = new SolidColorBrush(_backgroundColour),
                    Child = label
                };

                ((Border)element).SetBorderThickness(5);
            }

            if (_width > 0)
            {
                element.Width = _width;
            }

            return element;
        }

        public static UIElement Build(LabelBuilder builder)
        {
            return builder.Build();
        }
    }

    public class GuiBuilder
    {
        public const int DeviceWidth = 340;

        public const int Margin = 10;

        public const int HalfMargin = Margin / 2;

        public static Font Font { get; private set; }

        public delegate UIElement GuiBuilderEvent(GuiBuilder gui);

        public delegate ButtonBuilder ButtonBuilderEvent(ButtonBuilder button);

        public delegate LabelBuilder LabelBuilderEvent(LabelBuilder label);

        public delegate PanelBuilder PanelBuilderEvent(PanelBuilder panel);

        private GuiBuilder() { }

        public UIElement Button(ButtonBuilderEvent button)
        {
            var builder = button.Invoke(new ButtonBuilder());
            return ButtonBuilder.Build(builder);
        }

        public UIElement Image(byte[] imageBytes)
        {
            var bitmap = new Bitmap(imageBytes, Bitmap.BitmapImageType.Jpeg);
            var image = new Image(bitmap);
            return image;
        }

        public UIElement Panel(PanelBuilderEvent panel)
        {
            var builder = panel.Invoke(new PanelBuilder());
            return PanelBuilder.Build(builder);
        }

        public UIElement Label(LabelBuilderEvent label)
        {
            var builder = label.Invoke(new LabelBuilder());
            return LabelBuilder.Build(builder);
        }

        public static GuiBuilder Create()
        {
            if (Font == null)
            {
                Font = Resources.GetFont(Resources.FontResources.NinaB);
            }

            return new GuiBuilder();
        }
    }
}
