using Microsoft.VisualBasic.ApplicationServices;


namespace TfsBuildAnalyzer
{
    public class SingleInstanceManager : WindowsFormsApplicationBase
    {
        private TfsBuildAnalyzerApplication myApplication;
        private System.Collections.ObjectModel.ReadOnlyCollection<string> myCommandLine;

        public SingleInstanceManager()
        {
            IsSingleInstance = true;
        }

        protected override bool OnStartup(StartupEventArgs eventArgs)
        {
            // First time _application is launched
            myCommandLine = eventArgs.CommandLine;
            myApplication = new TfsBuildAnalyzerApplication();
            myApplication.InitializeAppComponent();
            myApplication.Run();
            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            // Subsequent launches
            base.OnStartupNextInstance(eventArgs);
            myCommandLine = eventArgs.CommandLine;
            myApplication.Activate(eventArgs);
        }
    }
}
