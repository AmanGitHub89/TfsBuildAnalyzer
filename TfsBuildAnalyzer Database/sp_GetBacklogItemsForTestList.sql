USE [TfsBuildAnalyzer]
GO

IF EXISTS (select 1 from sys.procedures where name='sp_GetBacklogItemsForTestList')
BEGIN
	DROP PROCEDURE	sp_GetBacklogItemsForTestList
END
GO

CREATE	PROCEDURE sp_GetBacklogItemsForTestList
(
	@TestIds	TestList	READONLY
)
AS
BEGIN
	SELECT	TI.TestId,
			BacklogId,
			BacklogTitle
	FROM	BacklogItems	BI
	JOIN	@TestIds		TI
	ON		BI.TestId = TI.TestId
END