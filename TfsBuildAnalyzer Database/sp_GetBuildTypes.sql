USE [TfsBuildAnalyzer]
GO

IF EXISTS (select 1 from sys.procedures where name='sp_GetBuildTypes')
BEGIN
	DROP PROCEDURE	sp_GetBuildTypes
END
GO

CREATE	PROCEDURE sp_GetBuildTypes
AS
BEGIN
	SELECT	BuildType
	FROM	BuildTypes
	ORDER BY	BuildType
END