USE [TfsBuildAnalyzer]
GO

IF EXISTS (select 1 from sys.procedures where name='sp_InsertResults')
BEGIN
	DROP PROCEDURE	sp_InsertResults
END
GO

CREATE	PROCEDURE sp_InsertResults
(
	@BuildType		VARCHAR(100),
	@BuildNumber	VARCHAR(100),
	@BuildStatus	VARCHAR(30),
	@BuildUrl		VARCHAR(1000),
	@BuildDate		DATETIME,
	@PassedCount	INTEGER,
	@FailedCount	INTEGER,
	@IgnoredCount	INTEGER,
	@TestResults	TestResult	READONLY
)
AS
BEGIN

	DECLARE @Error				INTEGER
	DECLARE @RowCount			INTEGER
	DECLARE	@InsertedTestIds	TABLE
	(
		TestId		UNIQUEIDENTIFIER,
		TestName	NVARCHAR(MAX),
		NewTestId	UNIQUEIDENTIFIER
	)

	IF EXISTS (SELECT 1 FROM BuildInfo WHERE BuildType = @BuildType AND BuildNumber = @BuildNumber)
	BEGIN
		SELECT	-1
		RETURN
	END

	BEGIN TRAN

	INSERT	INTO	@InsertedTestIds
	(
		TestId
	)
	SELECT	TestId
	FROM	@TestResults

	IF	@@ERROR <> 0
	BEGIN
		GOTO ErrorOccured
	END

	IF NOT EXISTS(SELECT 1 FROM BuildTypes WHERE BuildType = @BuildType)
	BEGIN
		INSERT INTO BuildTypes
		VALUES (@BuildType)

		SELECT	@Error = @@ERROR, @RowCount = @@ROWCOUNT

		IF	@Error <> 0 OR @RowCount <> 1
		BEGIN
			GOTO ErrorOccured
		END
	END

	INSERT INTO BuildInfo
	(
		BuildType,
		BuildNumber,
		BuildStatus,
		BuildUrl,
		BuildDate,
		PassedCount,
		FailedCount,
		IgnoredCount
	)
	VALUES
	(
		@BuildType,
		@BuildNumber,
		@BuildStatus,
		@BuildUrl,
		@BuildDate,
		@PassedCount,
		@FailedCount,
		@IgnoredCount
	)
	SELECT	@Error = @@ERROR, @RowCount = @@ROWCOUNT

	IF	@Error <> 0 OR @RowCount <> 1
	BEGIN
		GOTO ErrorOccured
	END

	--If test does not exist, new test id is same as passed test id; else picked from TestIndex table to return to application.
	INSERT	INTO	@InsertedTestIds
	(
		TestName,
		TestId,
		NewTestId
	)
	SELECT		DISTINCT(TR.TestName),
				TR.TestId,
				CASE(ISNULL(TI.TestName, ''))
				WHEN	''	THEN	TR.TestId
								ELSE	TI.TestId
				END
	FROM		@TestResults	TR
	LEFT JOIN	TestIndex		TI
	ON			HASHBYTES('SHA2_256',TR.TestName) = HASHBYTES('SHA2_256',TI.TestName)

	DELETE	FROM	@InsertedTestIds
	WHERE	TestId		IS NULL
	OR		NewTestId	IS NULL

	IF	(SELECT COUNT(1) FROM @InsertedTestIds) <> (SELECT COUNT(DISTINCT(TestName)) FROM @TestResults)
	BEGIN
		GOTO ErrorOccured
	END

	SELECT	@Error = @@ERROR
	IF	@Error <> 0
	BEGIN
		GOTO ErrorOccured
	END

	INSERT		INTO TestIndex
	(
		TestId,
		TestName
	)
	SELECT		TestId,
				TestName
	FROM		@InsertedTestIds
	WHERE		TestId = NewTestId

	SELECT	@Error = @@ERROR
	IF	@Error <> 0
	BEGIN
		GOTO ErrorOccured
	END

	
	INSERT		INTO FilePath
	SELECT		DISTINCT(TR.FilePath)
	FROM		@TestResults	TR
	LEFT JOIN	FilePath		FP
	ON			HASHBYTES('SHA2_256',TR.FilePath) = HASHBYTES('SHA2_256',FP.FilePath)
	WHERE		FP.FilePath IS NULL

	SELECT	@Error = @@ERROR
	IF	@Error <> 0
	BEGIN
		GOTO ErrorOccured
	END

	INSERT		INTO ExceptionMessage
	SELECT		DISTINCT(TR.ExceptionMessage)
	FROM		@TestResults		TR
	LEFT JOIN	ExceptionMessage	EM
	ON			HASHBYTES('SHA2_256',TR.ExceptionMessage) = HASHBYTES('SHA2_256',EM.ExceptionMessage)
	WHERE		EM.ExceptionMessage IS NULL
	AND			TR.ExceptionMessage IS NOT NULL
	AND			TR.ExceptionMessage <> ''

	SELECT	@Error = @@ERROR
	IF	@Error <> 0
	BEGIN
		GOTO ErrorOccured
	END

	INSERT		INTO ExceptionTrace
	SELECT		DISTINCT(TR.ExceptionTrace)
	FROM		@TestResults	TR
	LEFT JOIN	ExceptionTrace	ET
	ON			HASHBYTES('SHA2_256',TR.ExceptionTrace) = HASHBYTES('SHA2_256',ET.ExceptionTrace)
	WHERE		ET.ExceptionTrace IS NULL
	AND			TR.ExceptionTrace IS NOT NULL
	AND			TR.ExceptionTrace <> ''

	SELECT	@Error = @@ERROR
	IF	@Error <> 0
	BEGIN
		GOTO ErrorOccured
	END

	INSERT INTO TestResults
	(
		BuildType,
		BuildNumber,
		TestId,
		TestAgent,
		ExceptionMessageId,
		ExceptionTraceId,
		TestStatus,
		FilePathId,
		Duration
	)
	SELECT		@BuildType,
				@BuildNumber,
				TI.TestId,
				TestAgent,
				EM.ExceptionId,
				ET.ExceptionId,
				TestStatus,
				FP.Id,
				Duration
	FROM		@TestResults	TR
	JOIN		TestIndex		TI
	ON			TR.TestName = TI.TestName
	JOIN		FilePath		FP
	ON			TR.FilePath = FP.FilePath
	LEFT JOIN	ExceptionMessage	EM
	ON			TR.ExceptionMessage = EM.ExceptionMessage
	LEFT JOIN	ExceptionTrace	ET
	ON			TR.ExceptionTrace = ET.ExceptionTrace

	SELECT	@Error = @@ERROR, @RowCount = @@ROWCOUNT
	IF	@Error <> 0 OR @RowCount < 1
	BEGIN
		GOTO ErrorOccured
	END

	COMMIT TRAN

	SELECT	1

	SELECT	TestId,
			NewTestId,
			dbo.BacklogItemExists(NewTestId) AS HasItems
	FROM	@InsertedTestIds
	RETURN

	ErrorOccured:
	ROLLBACK TRAN
	SELECT	-1
END
