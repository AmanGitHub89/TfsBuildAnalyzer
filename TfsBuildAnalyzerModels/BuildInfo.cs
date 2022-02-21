using System;
using System.Collections.Generic;


namespace TfsBuildAnalyzerModels
{
    public class BuildInfo
    {
        public string BuildType { get; set; }
        public string BuildNumber { get; set; }
        public string BuildStatus { get; set; } = "None";
        public string BuildUrl { get; set; } = string.Empty;
        public string BuildDropLocation { get; set; }
        public DateTime BuildDate { get; set; } = DateTime.MinValue;
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public int IgnoredCount { get; set; }
        public bool IsSuccess => FailedCount == 0 && IgnoredCount == 0;
        public List<TestResult> TestResults { get; set; }
    }
}
