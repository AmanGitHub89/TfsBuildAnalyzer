USE [TfsBuildAnalyzer]
GO

IF EXISTS (select 1 from sys.procedures where name='sp_DeleteResult')
BEGIN
	DROP PROCEDURE	sp_DeleteResult
END
GO

CREATE	PROCEDURE sp_DeleteResult
(
	@BuildType		VARCHAR(100),
	@BuildNumber	VARCHAR(100)
)
AS
BEGIN
	DECLARE @Error				INTEGER
	DECLARE @RowCount			INTEGER

	IF NOT EXISTS (SELECT 1 FROM BuildInfo WHERE BuildType = @BuildType AND BuildNumber = @BuildNumber)
	BEGIN
		GOTO FinishWithSuccess
	END

	BEGIN TRAN

	DELETE	FROM TestResults
	WHERE	BuildType = @BuildType
	AND		BuildNumber = @BuildNumber

	IF	@Error <> 0 OR @RowCount < 1
	BEGIN
		GOTO FinishWithError
	END

	DELETE	FROM BuildInfo
	WHERE	BuildType = @BuildType
	AND		BuildNumber = @BuildNumber

	IF	@Error <> 0 OR @RowCount <> 1
	BEGIN
		GOTO FinishWithError
	END

	IF NOT EXISTS(SELECT 1 FROM BuildInfo WHERE BuildType = @BuildType)
	BEGIN
		DELETE FROM BuildTypes
		WHERE	BuildType = @BuildType
	END


	FinishWithSuccess:
	COMMIT TRAN
	SELECT	1
	RETURN

	FinishWithError:
	ROLLBACK TRAN
	SELECT	-1
END
