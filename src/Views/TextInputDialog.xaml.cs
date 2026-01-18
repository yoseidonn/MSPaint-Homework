using System.Windows;

namespace MSPaint.Views
{
    /// <summary>
    /// Interaction logic for TextInputDialog.xaml
    /// </summary>
    public partial class TextInputDialog : Window
    {
        public string Text => TextInputBox?.Text ?? string.Empty;
        public int FontSize { get; private set; } = 12;
        private bool _isInitializing = true;

        public TextInputDialog(int initialFontSize = 12)
        {
            InitializeComponent();
            _isInitializing = true;
            FontSize = initialFontSize;
            
            // Set values after InitializeComponent to avoid null reference
            if (FontSizeSlider != null)
            {
                FontSizeSlider.Value = initialFontSize;
            }
            if (FontSizeLabel != null)
            {
                FontSizeLabel.Content = initialFontSize.ToString();
            }
            
            // Focus the text input box
            if (TextInputBox != null)
            {
                TextInputBox.Focus();
                TextInputBox.SelectAll();
            }
            
            UpdatePreview();
            _isInitializing = false;
        }

        private void TextInputBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!_isInitializing && TextInputBox != null)
            {
                UpdatePreview();
            }
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            
            FontSize = (int)e.NewValue;
            if (FontSizeLabel != null)
            {
                FontSizeLabel.Content = FontSize.ToString();
            }
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (PreviewTextBlock != null && TextInputBox != null)
            {
                PreviewTextBlock.Text = string.IsNullOrEmpty(TextInputBox.Text) ? "Sample Text" : TextInputBox.Text;
                PreviewTextBlock.FontSize = FontSize;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            // Only close if there's text entered
            if (!string.IsNullOrWhiteSpace(TextInputBox?.Text))
            {
                DialogResult = true;
                Close();
            }
            else
            {
                // Show message if no text entered
                System.Windows.MessageBox.Show("Please enter some text.", "No Text", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
