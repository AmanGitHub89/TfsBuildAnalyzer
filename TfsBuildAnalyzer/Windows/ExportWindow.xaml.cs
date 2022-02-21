using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.Windows
{
    /// <summary>
    /// Interaction logic for ExportWindow.xaml
    /// </summary>
    public partial class ExportWindow
    {
        private bool myIsExporting;

        public ExportWindow()
        {
            InitializeComponent();
        }

        public void Export<T>(List<T> exportObjectList) where T : class
        {
            ProgressControl1.SetStepLabel("Exporting to excel...");
            var asyncWorker = new AsyncWorker(onDoWork: (o, progressAction) =>
            {
                try
                {
                    myIsExporting = true;
                    Thread.Sleep(20);
                    progressAction(0, null);
                    ExportToExcel.Export(exportObjectList, progressAction);
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
                finally
                {
                    myIsExporting = false;
                }
            }, data: null)
            {
                ProgressChanged = (sender, progress) => { ProgressChanged(progress, sender); },
                OnComplete = (sender, o) => { Close(); }
            };

            asyncWorker.RunAsync();
        }

        private void ProgressChanged(int progress, object sender)
        {
            ProgressControl1.SetProgress(progress);
        }

        private void ExportWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = myIsExporting;
        }
    }
}
