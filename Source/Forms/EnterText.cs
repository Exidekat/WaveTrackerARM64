using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WaveTracker.Forms
{
    public class EnterText : Window
    {
        public string Result { get; private set; }

        public EnterText()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OkButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Get the text from the TextBox
            Result = this.FindControl<TextBox>("TextBoxInput").Text;
            this.Close();
        }

        private void CancelButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Set the result to canceled and close
            Result = "\tcanceled";
            this.Close();
        }
    }
}