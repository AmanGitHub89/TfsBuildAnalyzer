using Microsoft.VisualBasic.ApplicationServices;

using TfsBuildAnalyzer.Windows;

using StartupEventArgs = System.Windows.StartupEventArgs;


namespace TfsBuildAnalyzer
{
    /// <summary>
    /// Interaction logic for TfsBuildAnalyzerApplication.xaml
    /// </summary>
    public partial class TfsBuildAnalyzerApplication
    {
        private static MainWindow myMainWindow;

        public void InitializeAppComponent()
        {
            Startup += Application_Startup;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            myMainWindow = new MainWindow();
            myMainWindow.Show();
        }

        public void Activate(StartupNextInstanceEventArgs eventArgs)
        {
            myMainWindow.Activate();
        }

        internal static void ShowBuildHistory(string buildType)
        {
            myMainWindow.Activate();
            myMainWindow.Reload(buildType);
        }
    }
}
