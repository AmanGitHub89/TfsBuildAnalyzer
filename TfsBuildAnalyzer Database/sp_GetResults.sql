USE [TfsBuildAnalyzer]
GO

IF EXISTS (select 1 from sys.procedures where name='sp_GetResults')
BEGIN
	DROP PROCEDURE	sp_GetResults
END
GO

CREATE	PROCEDURE sp_GetResults
(
	@BuildType		VARCHAR(100),
	@BuildNumber	VARCHAR(100)
)
AS
BEGIN
	SELECT	BuildType,
			BuildNumber,
			BuildStatus,
			BuildUrl,
			PassedCount,
			FailedCount,
			IgnoredCount
	FROM	BuildInfo
	WHERE	BuildType = @BuildType
	AND		BuildNumber = @BuildNumber

	SELECT		TI.TestId,
				TI.TestName,
				BuildType,
				BuildNumber,
				TestAgent,
				EM.ExceptionMessage,
				ET.ExceptionTrace,
				TestStatus,
				FP.FilePath,
				Duration,
				dbo.BacklogItemExists(TR.TestId) AS HasItems
	FROM		TestResults			TR
	JOIN		TestIndex			TI
	ON			TR.TestId = TI.TestId
	JOIN		FilePath			FP
	ON			TR.FilePathId = FP.Id
	LEFT JOIN	ExceptionMessage	EM
	ON			TR.ExceptionMessageId = EM.ExceptionId
	LEFT JOIN	ExceptionTrace		ET
	ON			TR.ExceptionTraceId = ET.ExceptionId
	WHERE		TR.BuildType = @BuildType
	AND			BuildNumber = @BuildNumber
END