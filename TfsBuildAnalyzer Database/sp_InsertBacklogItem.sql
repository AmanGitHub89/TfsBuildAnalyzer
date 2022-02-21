USE [TfsBuildAnalyzer]
GO

IF EXISTS (select 1 from sys.procedures where name='sp_InsertBacklogItem')
BEGIN
	DROP PROCEDURE	sp_InsertBacklogItem
END
GO

CREATE	PROCEDURE sp_InsertBacklogItem
(
	@TestId			UNIQUEIDENTIFIER,
	@BacklogId		INTEGER,
	@BacklogTitle	NVARCHAR(MAX)
)
AS
BEGIN
	IF	EXISTS	(
					SELECT	1
					FROM	BacklogItems
					WHERE	BacklogId = @BacklogId
					AND		TestId = @TestId
				)
	BEGIN
		SELECT	0
		RETURN
	END

	INSERT INTO  BacklogItems
	(
		BacklogId,
		TestId,
		BacklogTitle
	)
	VALUES
	(
		@BacklogId,
		@TestId,
		@BacklogTitle
	)
	
	IF	@@ERROR = 0 AND @@ROWCOUNT = 1
	BEGIN
		SELECT	1
	END
	ELSE
	BEGIN
		SELECT	-1
	END
END