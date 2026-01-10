using System.Windows;

namespace MSPaint.Pages
{
    /// <summary>
    /// Interaction logic for TextInputDialog.xaml
    /// </summary>
    public partial class TextInputDialog : Window
    {
        public string Text => TextInputBox.Text;
        public new int FontSize => (int)FontSizeSlider.Value;
        public bool Result { get; private set; }

        public TextInputDialog(int initialFontSize = 12)
        {
            InitializeComponent();
            FontSizeSlider.Value = initialFontSize;
            UpdatePreview();
            TextInputBox.Focus();
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            FontSizeLabel.Content = ((int)e.NewValue).ToString();
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            PreviewTextBlock.FontSize = FontSizeSlider.Value;
            PreviewTextBlock.Text = string.IsNullOrWhiteSpace(TextInputBox.Text) ? "Sample Text" : TextInputBox.Text;
        }

        private void TextInputBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            DialogResult = false;
            Close();
        }
    }
}
