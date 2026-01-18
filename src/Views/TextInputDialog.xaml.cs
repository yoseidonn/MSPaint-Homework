using System.Windows;

namespace MSPaint.Views
{
    /// <summary>
    /// Interaction logic for TextInputDialog.xaml
    /// </summary>
    public partial class TextInputDialog : Window
    {
        public string Text => TextInputBox.Text;
        public int FontSize { get; private set; } = 12;

        public TextInputDialog(int initialFontSize = 12)
        {
            InitializeComponent();
            FontSize = initialFontSize;
            FontSizeSlider.Value = initialFontSize;
            FontSizeLabel.Content = initialFontSize.ToString();
            UpdatePreview();
        }

        private void TextInputBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            FontSize = (int)e.NewValue;
            FontSizeLabel.Content = FontSize.ToString();
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            PreviewTextBlock.Text = string.IsNullOrEmpty(TextInputBox.Text) ? "Sample Text" : TextInputBox.Text;
            PreviewTextBlock.FontSize = FontSize;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
