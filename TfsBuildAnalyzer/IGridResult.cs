using System;


namespace TfsBuildAnalyzer
{
    public interface IGridResult
    {
        Guid TestId { get; set; }

        string TestName { get; set; }

        string BuildType { get; set; }

        string BuildNumber { get; set; }

        bool HasItems { get; set; }
    }
}
