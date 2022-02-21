USE [TfsBuildAnalyzer]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'FN' AND name = 'BacklogItemExists')
	DROP FUNCTION BacklogItemExists
GO

CREATE FUNCTION BacklogItemExists (@TestId UNIQUEIDENTIFIER)
RETURNS BIT
AS 
BEGIN
	DECLARE	@Exists	BIT
	IF EXISTS (SELECT 1 FROM BacklogItems WHERE TestId = @TestId)
	BEGIN
		SELECT	@Exists = 1
	END
	ELSE
	BEGIN
		SELECT	@Exists = 0
	END

	RETURN @Exists
END
GO