using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

        // Default constructor
        public Button()
        {
            avaloniaButton = new Avalonia.Controls.Button();
            avaloniaButton.Click += OnAvaloniaButtonClick;
        }

        // Constructor accepting label, x, y, width, and parent
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

        private void OnAvaloniaButtonClick(object sender, RoutedEventArgs e)
        {
            // This will handle the Click event in the in-code usage
            OnClick();
        }

        public event EventHandler<RoutedEventArgs> Click
        {
            add => avaloniaButton.Click += value;
            remove => avaloniaButton.Click -= value;
        }

        public void SetLabel(string label)
        {
            Label = label;
            avaloniaButton.Content = label;
            labelWidth = Helpers.GetWidthOfText(label);
        }

        public object Content
        {
            get => avaloniaButton.Content;
            set => avaloniaButton.Content = value;
        }

        public void Draw()
        {
            avaloniaButton.Background = GetBackgroundColor().ToAvaloniaBrush();
            avaloniaButton.BorderBrush = GetBorderColor().ToAvaloniaBrush();
        }

        private Color GetBackgroundColor()
        {
            return IsPressed ? colors.backgroundColorPressed : IsHovered ? colors.backgroundColorHover : colors.backgroundColor;
        }

        private Color GetBorderColor()
        {
            return IsPressed ? colors.borderColorPressed : colors.borderColor;
        }
    }

    public static class ColorExtensions
    {
        // Convert XNA Color to Avalonia Brush
        public static Avalonia.Media.IBrush ToAvaloniaBrush(this Microsoft.Xna.Framework.Color color)
        {
            return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }
    }
}
