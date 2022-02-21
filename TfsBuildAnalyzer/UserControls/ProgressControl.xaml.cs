using System.Windows.Controls;


namespace TfsBuildAnalyzer.UserControls
{
    /// <summary>
    /// Interaction logic for ProgressControl.xaml
    /// </summary>
    public partial class ProgressControl : UserControl
    {
        public ProgressControl()
        {
            InitializeComponent();
        }

        public void SetProgress(int progress)
        {
            ProgressLabel.Content = $"{progress} %";
            ProgressBar1.Value = progress;
        }

        public void SetStepLabel(string step)
        {
            StepLabel.Content = step;
        }
    }
}
