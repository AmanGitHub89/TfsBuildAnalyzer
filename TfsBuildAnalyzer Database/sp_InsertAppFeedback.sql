USE [TfsBuildAnalyzer]
GO

IF EXISTS (select 1 from sys.procedures where name='sp_InsertAppFeedback')
BEGIN
	DROP PROCEDURE	sp_InsertAppFeedback
END
GO

CREATE	PROCEDURE sp_InsertAppFeedback
(
	@FeedbackDescription	NVARCHAR(MAX),
	@UserInfo				VARCHAR(200) = NULL
)
AS
BEGIN
	INSERT INTO  AppFeedback
	(
		FeedbackId,
		FeedbackDescription,
		FeedbackUser
	)
	VALUES
	(
		NEWID(),
		@FeedbackDescription,
		@UserInfo
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