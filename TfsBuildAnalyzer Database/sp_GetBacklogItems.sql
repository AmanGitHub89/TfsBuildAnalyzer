USE [TfsBuildAnalyzer]
GO

IF EXISTS (select 1 from sys.procedures where name='sp_GetBacklogItems')
BEGIN
	DROP PROCEDURE	sp_GetBacklogItems
END
GO

CREATE	PROCEDURE sp_GetBacklogItems
(
	@TestId	UNIQUEIDENTIFIER
)
AS
BEGIN
	SELECT	BacklogId,
			BacklogTitle
	FROM	BacklogItems
	WHERE	TestId = @TestId
END