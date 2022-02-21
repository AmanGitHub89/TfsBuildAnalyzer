/* -------------------------------------------------------------------------------------------------
   Restricted - Copyright (C) Siemens Healthcare GmbH/Siemens Medical Solutions USA, Inc., 2020. All rights reserved
   ------------------------------------------------------------------------------------------------- */

using System;
using System.ComponentModel;


namespace TfsBuildAnalyzer.Utilities
{
    internal class AsyncWorker
    {
        private BackgroundWorker myBackgroundWorker;

        private readonly Action<object, Action<int, object>> myOnDoWorkAction;
        private readonly object myData;

        public EventHandler<int> ProgressChanged;
        public EventHandler<object> OnComplete;

        public AsyncWorker(Action<object, Action<int, object>> onDoWork, object data)
        {
            myOnDoWorkAction = onDoWork;
            myData = data;
        }

        public void RunAsync()
        {
            myBackgroundWorker = new BackgroundWorker();
            myBackgroundWorker.DoWork += OnDoWork;
            myBackgroundWorker.ProgressChanged += OnProgressChanged;
            myBackgroundWorker.RunWorkerCompleted += OnRunWorkerCompleted;
            myBackgroundWorker.WorkerReportsProgress = true;
            myBackgroundWorker.RunWorkerAsync();
        }

        private void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OnComplete?.Invoke(this, e.Result);
        }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressChanged?.Invoke(this, e.ProgressPercentage);
        }

        private void OnDoWork(object sender, DoWorkEventArgs e)
        {
            myOnDoWorkAction(myData, (progress, data) =>
            {
                e.Result = data;
                myBackgroundWorker.ReportProgress(progress);
            });
        }
    }
}
