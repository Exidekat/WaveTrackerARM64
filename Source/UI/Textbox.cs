using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;

namespace WaveTracker.UI {
    public class Textbox : UserControl {
        private Window dialog;
        public bool canEdit = true;
        private string label;
        private string textPrefix = "";
        private int textboxWidth;
        public bool ValueWasChanged { get; private set; }
        public bool ValueWasChangedInternally { get; set; }
        public string Text { get; set; }

        private string lastText;
        public int MaxLength { get; set; }

        public Textbox(string label, int x, int y, int width, int textBoxWidth) {
            this.label = label;
            this.Margin = new Thickness(x, y, 0, 0);
            this.Width = width;
            textboxWidth = textBoxWidth;
            Height = 30;
            MaxLength = 32;

            this.Text = string.Empty;
        }

        public Textbox(string label, int x, int y, int width) {
            this.label = label;
            this.Margin = new Thickness(x, y, 0, 0);
            this.Width = width;
            textboxWidth = label == "" ? width : width - Helpers.GetWidthOfText(label) - 4;
            Height = 30;
            MaxLength = 32;

            this.Text = string.Empty;
        }

        public void SetPrefix(string prefix) {
            textPrefix = prefix;
        }

        public void Update() {
            if (canEdit) {
                ValueWasChangedInternally = false;

                if (ValueWasChanged || !string.Equals(Text, lastText, StringComparison.Ordinal)) {
                    ValueWasChanged = true;
                    lastText = Text;
                }
                else {
                    ValueWasChanged = false;
                }
            }
        }

        public void StartDialog() {
            if (dialog != null) return;

            dialog = new Window {
                Width = 300,
                Height = 150,
                Title = "Edit Text"
            };

            var textBox = new TextBox { Text = Text, MaxLength = MaxLength, Margin = new Thickness(10) };
            var okButton = new Button { Content = "OK", Width = 100, Margin = new Thickness(10) };
            okButton.Click += (s, e) => {
                Text = textBox.Text;
                ValueWasChangedInternally = true;
                dialog.Close();
                dialog = null;
            };

            var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(okButton);
            dialog.Content = stackPanel;

            dialog.ShowDialog(App.MainWindow);
        }

        public override void Render(DrawingContext context) {
            base.Render(context);

            var darkBrush = new SolidColorBrush(Colors.DarkGray);
            var textBrush = new SolidColorBrush(Colors.Black);
            var backgroundBrush = new SolidColorBrush(Colors.White);

            var textBoxRect = new Rect(Bounds.Width - textboxWidth, 0, textboxWidth, Bounds.Height);

            // Draw Label
            var formattedLabel = new FormattedText {
                Text = label,
                Typeface = new Typeface("Arial"),
                FontSize = 12
            };
            context.DrawText(textBrush, new Point(0, Bounds.Height / 2 - formattedLabel.Bounds.Height / 2), formattedLabel);

            // Draw Textbox
            context.DrawRectangle(darkBrush, new Pen(Brushes.Gray, 1), textBoxRect);
            context.DrawRectangle(backgroundBrush, null, new Rect(textBoxRect.X + 1, textBoxRect.Y + 1, textBoxRect.Width - 2, textBoxRect.Height - 2));

            // Display text inside the textbox
            var displayText = textPrefix + Text;
            var formattedText = new FormattedText {
                Text = displayText.Length > MaxLength ? displayText.Substring(0, MaxLength) : displayText,
                Typeface = new Typeface("Arial"),
                FontSize = 12
            };
            var textPosition = new Point(textBoxRect.Left + 4, Bounds.Height / 2 - formattedText.Bounds.Height / 2);
            context.DrawText(textBrush, textPosition, formattedText);
        }
    }
}
