using System;

using TfsBuildAnalyzerModels;


namespace TfsBuildAnalyzer
{
    public class BacklogItemAddedEventArgs : EventArgs
    {
        public TestBacklogItem Item { get; set; }
    }
}
