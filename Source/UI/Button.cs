using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;

#nullable enable

namespace WaveTracker.UI
{
    public class Button : Avalonia.Controls.Button
    {
        public string Label { get; set; }
        public int x, y, width;
        public bool LabelIsCentered;
        public ButtonColors colors;
        public Avalonia.Controls.Button avaloniaButton;
        
        // Boolean flag to track button clicks
        private bool _clicked;

        public Button(string label, int x, int y, int width, Element? parent = null)
        {
            Label = label;
            this.x = x;
            this.y = y;
            this.width = width;
            LabelIsCentered = true;
            colors = ButtonColors.Round;
            SetParent(parent);

            // Create the Avalonia button instance
            avaloniaButton = new Avalonia.Controls.Button
            {
                Content = label,
                Width = width
            };
            avaloniaButton.Click += OnAvaloniaButtonClick;
        }

        // Event handler for Avalonia Button click
        private void OnAvaloniaButtonClick(object? sender, RoutedEventArgs e)
        {
            _clicked = true; // Set the flag when the button is clicked
            OnClick();
        }

        // Clicked property to be used like a boolean check
        public bool Clicked
        {
            get
            {
                bool wasClicked = _clicked;
                _clicked = false; // Reset the flag after returning the value
                return wasClicked;
            }
        }

        public new event EventHandler<RoutedEventArgs> Click
        {
            add => avaloniaButton.Click += value;
            remove => avaloniaButton.Click -= value;
        }

        public new object Content
        {
            get => avaloniaButton.Content;
            set => avaloniaButton.Content = value;
        }

        // Properties for Width, Height, and Margin
        public new double Width
        {
            get => avaloniaButton.Width;
            set => avaloniaButton.Width = value;
        }

        public new double Height
        {
            get => avaloniaButton.Height;
            set => avaloniaButton.Height = value;
        }

        public new Thickness Margin
        {
            get => avaloniaButton.Margin;
            set => avaloniaButton.Margin = value;
        }

        // New property to enable or disable the button
        public new bool IsEnabled
        {
            get => avaloniaButton.IsEnabled;
            set => avaloniaButton.IsEnabled = value;
        }

        // Create the 'enabled' property to map to 'IsEnabled'
        public bool enabled
        {
            get => this.IsEnabled;
            set => this.IsEnabled = value;
        }

        // Render method to handle custom rendering
        public new void Render(DrawingContext context)
        {
            // Use Avalonia's rendering system for drawing
            avaloniaButton.Render(context);
        }
        
        // Implement SetTooltip method
        public void SetTooltip(string tooltipText)
        {
            ToolTip.SetTip(this, tooltipText);
        }

        public void SetLabel(string label)
        {
            Label = label;
            avaloniaButton.Content = label;
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
