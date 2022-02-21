namespace TfsBuildAnalyzer.Windows
{
    /// <summary>
    /// Interaction logic for TextDetailWindow.xaml
    /// </summary>
    public partial class TextDetailWindow
    {
        public TextDetailWindow(string text)
        {
            InitializeComponent();
            TextBox1.Text = text;
        }

        public void SetTitle(string title)
        {
            Title = title;
        }
    }
}
