USE [TfsBuildAnalyzer]
GO

IF EXISTS (select 1 from sys.procedures where name='sp_GetTestHistory')
BEGIN
	DROP PROCEDURE	sp_GetTestHistory
END
GO

CREATE	PROCEDURE sp_GetTestHistory
(
	@TestId			UNIQUEIDENTIFIER,
	@BuildType		VARCHAR(100),
	@BuildNumber	VARCHAR(100)
)
AS
BEGIN

	DECLARE	@BuildDate	DATETIME

	SELECT	@BuildDate = BuildDate
	FROM	BuildInfo
	WHERE	BuildType = @BuildType
	AND		BuildNumber = @BuildNumber

	SELECT		@TestId AS TestId,
				TestAgent,
				EM.ExceptionMessage,
				ET.ExceptionTrace,
				TestStatus,
				BI.BuildType,
				BI.BuildNumber,
				BI.BuildDate,
				FP.FilePath
	FROM		TestResults	TR
	JOIN		BuildInfo BI
	ON			TR.BuildType = BI.BuildType
	AND			TR.BuildNumber = BI.BuildNumber
	LEFT JOIN	ExceptionMessage EM
	ON			TR.ExceptionMessageId = EM.ExceptionId
	LEFT JOIN	ExceptionTrace ET
	ON			TR.ExceptionTraceId = ET.ExceptionId
	JOIN		FilePath		FP
	ON			TR.FilePathId = FP.Id
	WHERE		TR.TestId = @TestId
	AND			TR.BuildType = @BuildType
	AND			BuildDate <= @BuildDate
	ORDER BY	BuildDate DESC
END