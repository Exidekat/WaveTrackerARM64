using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;

namespace WaveTracker.UI {
    public class NumberBoxDecimal : UserControl {
        private Window dialog;
        private bool dialogOpen;

        public Button bUp;
        public Button bDown;
        private int boxWidth;
        private string label;
        private float min = float.MinValue;
        private float max = float.MaxValue;
        private int valueSaved;
        private bool canScroll = true;

        public enum DisplayMode { Number, Note, NoteOnly, PlusMinus }
        public DisplayMode displayMode = DisplayMode.Number;

        public bool ValueWasChanged { get; private set; }
        public bool ValueWasChangedInternally { get; private set; }

        private int lastValue;
        private int _value;

        private int DecimalPlaces { get; set; }

        public float Value {
            get { return _value / powersOfTen[DecimalPlaces]; }
            set { _value = (int)Math.Clamp(value * powersOfTen[DecimalPlaces], min * powersOfTen[DecimalPlaces], max * powersOfTen[DecimalPlaces]); }
        }

        private static int[] powersOfTen = { 1, 10, 100, 1000, 10000, 100000 };

        public NumberBoxDecimal(string label, int x, int y, int width, int boxWidth, int decimalPlaces = 2) {
            this.label = label;
            this.Margin = new Thickness(x, y, 0, 0);
            this.Width = width;
            this.boxWidth = boxWidth;
            DecimalPlaces = decimalPlaces;
            Height = 30;
            canScroll = true;

            bUp = new Button { Content = "▲", Width = 20, Height = 20, Margin = new Thickness(0, 0, 0, 10) };
            bUp.Click += (s, e) => Value++;

            bDown = new Button { Content = "▼", Width = 20, Height = 20 };
            bDown.Click += (s, e) => Value--;

            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(bUp);
            panel.Children.Add(bDown);

            this.Content = panel;
        }

        public void SetValueLimits(float min, float max) {
            this.min = min;
            this.max = max;
            if (Value < min) Value = min;
            if (Value > max) Value = max;
        }

        public void EnableScrolling() { canScroll = true; }
        public void DisableScrolling() { canScroll = false; }

        public void Update() {
            bUp.IsEnabled = Value < max;
            bDown.IsEnabled = Value > min;

            if (dialogOpen && !IsMouseOver) {
                dialogOpen = false;
            }

            if (IsPointerOver) {
                if (canScroll) {
                    this.AddHandler(PointerWheelChangedEvent, (s, e) => {
                        Value += (float)e.Delta.Y / powersOfTen[DecimalPlaces]; // Scroll to adjust value
                        e.Handled = true;
                    });
                }
            }

            ValueWasChangedInternally = _value != lastValue;
            lastValue = _value;
        }

        public void StartDialog() {
            if (!dialogOpen) {
                dialogOpen = true;

                dialog = new Window {
                    Width = 300,
                    Height = 150,
                    Title = "Enter Value"
                };

                var textBox = new TextBox { Text = Value.ToString(), Margin = new Thickness(10) };
                var okButton = new Button { Content = "OK", Width = 100, Margin = new Thickness(10) };
                okButton.Click += (s, e) => {
                    if (float.TryParse(textBox.Text, out float result)) {
                        Value = result;
                    }
                    dialog.Close();
                };

                var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
                stackPanel.Children.Add(textBox);
                stackPanel.Children.Add(okButton);
                dialog.Content = stackPanel;

                dialog.ShowDialog(App.MainWindow);
            }
        }

        public override void Render(DrawingContext context) {
            base.Render(context);
            var rectBrush = new SolidColorBrush(Colors.White);
            var borderBrush = new SolidColorBrush(Colors.Gray);
            var textBrush = new SolidColorBrush(Colors.Black);

            var boxRect = new Rect(Bounds.Width - boxWidth, 0, boxWidth - 20, Bounds.Height);

            // Draw Box
            context.DrawRectangle(rectBrush, new Pen(borderBrush, 1), boxRect);

            // Draw Text
            string textToDisplay = Value.ToString($"F{DecimalPlaces}");
            var formattedText = new FormattedText {
                Text = textToDisplay,
                Typeface = new Typeface("Arial"),
                FontSize = 12,
                TextAlignment = TextAlignment.Left
            };

            var textPoint = new Point(boxRect.Left + 5, (Bounds.Height - formattedText.Bounds.Height) / 2);
            context.DrawText(textBrush, textPoint, formattedText);

            // Draw buttons
            bUp.Render(context);
            bDown.Render(context);
        }
    }
}
