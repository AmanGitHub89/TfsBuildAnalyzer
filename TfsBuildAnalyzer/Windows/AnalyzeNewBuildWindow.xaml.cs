using System.IO;
using System.Windows;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.Windows
{
    /// <summary>
    /// Interaction logic for AnalyzeNewBuildWindow.xaml
    /// </summary>
    public partial class AnalyzeNewBuildWindow
    {
        public AnalyzeNewBuildWindow()
        {
            InitializeComponent();
        }

        private void AnalyzeButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dropFolderDirectory = DropFolderPathTextBox.Text.Trim();
            if (!Directory.Exists(dropFolderDirectory))
            {
                MessageBoxHelper.ShowErrorMessage("Invalid Directory", "The drop location does not exist.");
                return;
            }

            var buildType = Directory.GetParent(dropFolderDirectory).Name;
            var buildInputsInfo = new BuildInfo { BuildType = buildType, BuildDropLocation = dropFolderDirectory, BuildUrl = dropFolderDirectory};
            var canAnalyzeState = BuildDropFolderValidityChecker.CanAnalyze(buildInputsInfo);
            if (canAnalyzeState == CanAnalyzeState.CanAnalyze)
            {
                var analyzeDropFolderWindow = new AnalyzeDropFolderWindow(buildInputsInfo);
                analyzeDropFolderWindow.ShowDialog();
                Close();
            }
            else if (canAnalyzeState == CanAnalyzeState.BuildExists)
            {
                BuildDropFolderValidityChecker.LoadExistingBuild(buildInputsInfo.BuildType, buildInputsInfo.BuildNumber);
            }
            else
            {
                BuildDropFolderValidityChecker.ShowMessageForAnalyzeState(canAnalyzeState, buildInputsInfo);
            }
        }

        private void AnalyzeNewBuildWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            DropFolderPathTextBox.Focus();
        }
    }
}
