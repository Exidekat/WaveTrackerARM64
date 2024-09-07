using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Microsoft.Xna.Framework;

namespace WaveTracker.UI
{
    public class Button : Clickable
    {
        private Avalonia.Controls.Button avaloniaButton;
        public string Label { get; private set; }
        public bool LabelIsCentered { get; set; }
        private ButtonColors colors;
        private int labelWidth;

        public Button()
        {
            avaloniaButton = new Avalonia.Controls.Button();
            avaloniaButton.Click += OnAvaloniaButtonClick;
        }

        public Button(string label, int x, int y, int width, Element parent)
        {
            Label = label;
            this.x = x;
            this.y = y;
            this.width = width;
            LabelIsCentered = true;
            colors = ButtonColors.Round;
            SetParent(parent);

            avaloniaButton = new Avalonia.Controls.Button
            {
                Content = label
            };
            avaloniaButton.Click += OnAvaloniaButtonClick;
        }

        // Event handler for Avalonia Button click
        private void OnAvaloniaButtonClick(object sender, RoutedEventArgs e)
        {
            OnClick();
        }

        public event EventHandler<RoutedEventArgs> Click
        {
            add => avaloniaButton.Click += value;
            remove => avaloniaButton.Click -= value;
        }

        public object Content
        {
            get => avaloniaButton.Content;
            set => avaloniaButton.Content = value;
        }

        // New properties for Width, Height, and Margin
        public double Width
        {
            get => avaloniaButton.Width;
            set => avaloniaButton.Width = value;
        }

        public double Height
        {
            get => avaloniaButton.Height;
            set => avaloniaButton.Height = value;
        }

        public Thickness Margin
        {
            get => avaloniaButton.Margin;
            set => avaloniaButton.Margin = value;
        }

        public void SetLabel(string label)
        {
            Label = label;
            avaloniaButton.Content = label;
            labelWidth = Helpers.GetWidthOfText(label);
        }

        public void Draw()
        {
            avaloniaButton.Background = GetBackgroundColor().ToAvaloniaBrush();
            avaloniaButton.BorderBrush = GetBorderColor().ToAvaloniaBrush();
        }

        private Microsoft.Xna.Framework.Color GetBackgroundColor()
        {
            return IsPressed ? colors.backgroundColorPressed : IsHovered ? colors.backgroundColorHover : colors.backgroundColor;
        }

        private Microsoft.Xna.Framework.Color GetBorderColor()
        {
            return IsPressed ? colors.borderColorPressed : colors.borderColor;
        }
    }

    public static class ColorExtensions
    {
        public static IBrush ToAvaloniaBrush(this Microsoft.Xna.Framework.Color color)
        {
            return new SolidColorBrush(Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }
    }
}
