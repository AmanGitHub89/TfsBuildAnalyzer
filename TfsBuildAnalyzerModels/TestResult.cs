using System;


namespace TfsBuildAnalyzerModels
{
    public class TestResult
    {
        public Guid TestId { get; set; }

        [ExportToExcelAttribute]
        public string TestName { get; set; }

        [ExportToExcelAttribute]
        public string BuildType { get; set; }

        [ExportToExcelAttribute]
        public string BuildNumber { get; set; }

        [ExportToExcelAttribute]
        public string TestAgent { get; set; } = "";

        [ExportToExcelAttribute]
        public string ExceptionMessage { get; set; }

        [ExportToExcelAttribute]
        public string ExceptionTrace { get; set; }

        [ExportToExcelAttribute]
        public TestStatus TestStatus { get; set; }

        [ExportToExcelAttribute]
        public string Duration { get; set; }

        [ExportToExcelAttribute]
        public string FilePath { get; set; }

        public bool HasItems { get; set; }

        public int FailCount { get; set; }

        [ExcelCellColor]
        public string ExcelCellColor => TestStatus == TestStatus.Passed ? ResultColors.Passed : TestStatus == TestStatus.Ignored ? ResultColors.Ignored : ResultColors.Failed;
    }

    public enum TestStatus
    {
        Passed,
        Ignored,
        Failed
    }
}
