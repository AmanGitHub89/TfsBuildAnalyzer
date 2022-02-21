USE [TfsBuildAnalyzer]
GO

IF EXISTS (select 1 from sys.procedures where name='sp_GetBuildInfo')
BEGIN
	DROP PROCEDURE	sp_GetBuildInfo
END
GO

CREATE	PROCEDURE sp_GetBuildInfo
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
			BuildDate,
			PassedCount,
			FailedCount,
			IgnoredCount
	FROM	BuildInfo
	WHERE	BuildType = @BuildType
	AND		BuildNumber = @BuildNumber
END