using System;
using System.Collections.Generic;


namespace TfsBuildAnalyzerModels
{
    public class SaveResultsResponse
    {
        public bool IsSaved { get; set; }
        public List<OldVsNewTestId> OldVsNewTestIdList { get; set; }
    }

    public class OldVsNewTestId
    {
        public Guid TestId { get; set; }
        public Guid NewTestId { get; set; }
        public bool HasItems { get; set; }
    }
}
