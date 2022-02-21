USE [TfsBuildAnalyzer]
GO

IF EXISTS (select 1 from sys.procedures where name='sp_GetRecentBuilds')
BEGIN
	DROP PROCEDURE	sp_GetRecentBuilds
END
GO

CREATE	PROCEDURE sp_GetRecentBuilds
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
	WHERE	BuildDate >= DATEADD(dd, -180, SYSUTCDATETIME())	--Less than 6 months old
END