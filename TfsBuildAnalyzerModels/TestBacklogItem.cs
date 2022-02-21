using System;


namespace TfsBuildAnalyzerModels
{
    public class TestBacklogItem
    {
        public Guid TestId { get; set; }

        [ExportToExcel]
        public int BacklogId { get; set; }

        [ExportToExcel]
        public string BacklogTitle { get; set; }
    }
}
