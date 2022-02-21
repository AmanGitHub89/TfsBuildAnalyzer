USE [TfsBuildAnalyzer]
GO

IF EXISTS (select 1 from sys.procedures where name='sp_InsertAppIssue')
BEGIN
	DROP PROCEDURE	sp_InsertAppIssue
END
GO

CREATE	PROCEDURE sp_InsertAppIssue
(
	@IssueDescription	NVARCHAR(MAX),
	@UserInfo			VARCHAR(200) = NULL
)
AS
BEGIN
	INSERT INTO  AppIssues
	(
		IssueId,
		IssueDescription,
		IssueUser
	)
	VALUES
	(
		NEWID(),
		@IssueDescription,
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