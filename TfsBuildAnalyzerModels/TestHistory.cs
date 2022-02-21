using System;


namespace TfsBuildAnalyzerModels
{
    public class TestHistory
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

        public string ExceptionMessage { get; set; }

        public string ExceptionTrace { get; set; }

        [ExportToExcelAttribute]
        public string Error => $"{ExceptionMessage}{Environment.NewLine}{Environment.NewLine}{ExceptionTrace}";

        [ExportToExcelAttribute]
        public TestStatus TestStatus { get; set; }

        [ExportToExcelAttribute]
        public string FilePath { get; set; }

        [ExcelCellColor]
        public string ExcelCellColor => TestStatus == TestStatus.Passed ? ResultColors.Passed : TestStatus == TestStatus.Ignored ? ResultColors.Ignored : ResultColors.Failed;
    }
}
