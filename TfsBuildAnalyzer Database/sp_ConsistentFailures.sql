USE [TfsBuildAnalyzer]
GO

IF EXISTS (select 1 from sys.procedures where name='sp_ConsistentFailures')
BEGIN
	DROP PROCEDURE	sp_ConsistentFailures
END
GO

CREATE	PROCEDURE sp_ConsistentFailures
(
	@BuildType		VARCHAR(100),
	@FailureCount	SMALLINT
)
AS
BEGIN
	DECLARE		@BuildDate		DATETIME
	DECLARE		@BuildNumber	VARCHAR(100)
	DECLARE		@TestId			UNIQUEIDENTIFIER
	DECLARE		@TestFailCount	INTEGER

	DECLARE		@FailedTestIds	TABLE
	(
		TestId		UNIQUEIDENTIFIER,
		FailCount	INTEGER
	)
	DECLARE @Result TABLE 
	(
		BuildDate	DATETIME,
		TestId		UNIQUEIDENTIFIER,
		TestStatus	VARCHAR(10),
		RowNumber	INTEGER
	)

	--Get latest build for selected build type
	SELECT		TOP 1
				@BuildDate = BuildDate,
				@BuildNumber = BuildNumber
	FROM		BuildInfo
	WHERE		BuildType = @BuildType
	ORDER BY	BuildDate DESC

	--Get current failed test Ids(Ignored are also treated as failed here).
	INSERT	INTO	@FailedTestIds
	(
		TestId
	)
	SELECT		TI.TestId
	FROM		TestResults			TR
	JOIN		TestIndex			TI
	ON			TR.TestId = TI.TestId
	WHERE		TR.BuildType = @BuildType
	AND			TR.BuildNumber = @BuildNumber
	AND			TR.TestStatus <> 'Passed'

	DECLARE	FailedTestCursor CURSOR FOR
	SELECT	TestId
	FROM	@FailedTestIds
	OPEN	FailedTestCursor
	FETCH NEXT FROM FailedTestCursor
	INTO	@TestId
	WHILE @@FETCH_STATUS = 0
	BEGIN
		--Get test history for this test
		INSERT INTO @Result
		SELECT	BI.BuildDate,
				TR.TestId,
				TR.TestStatus,
				ROW_NUMBER() OVER (ORDER BY BI.BuildDate DESC) AS RowNumber
		FROM	TestResults	TR
		JOIN	BuildInfo	BI
		ON		TR.BuildType = BI.BuildType
		AND		TR.BuildNumber = BI.BuildNumber
		WHERE	BI.BuildType = @BuildType
		AND		TR.TestId = @TestId
		AND		BI.BuildDate < @BuildDate
		ORDER BY	RowNumber

		--Get Consistent failure count
		SELECT	@TestFailCount = MIN(RowNumber)
		FROM	@Result
		WHERE	TestStatus = 'Passed'

		IF	@TestFailCount IS NULL
		BEGIN
			SELECT	@TestFailCount = MAX(RowNumber)
			FROM	@Result

			SELECT	@TestFailCount = @TestFailCount + 1
		END

		UPDATE	@FailedTestIds
		SET		FailCount = @TestFailCount
		WHERE	TestId = @TestId

		DELETE	FROM	@Result

		FETCH NEXT FROM FailedTestCursor
		INTO @TestId		
	END
	CLOSE FailedTestCursor -- close the cursor
	DEALLOCATE FailedTestCursor -- Deallocate the cursor

	DELETE	FROM	@FailedTestIds
	WHERE	FailCount	IS	NULL
	OR		FailCount < @FailureCount
	
	SELECT		TI.TestId,
				TI.TestName,
				TR.BuildType,
				TR.BuildNumber,
				TestAgent,
				EM.ExceptionMessage,
				ET.ExceptionTrace,
				TestStatus,
				FP.FilePath,
				dbo.BacklogItemExists(TR.TestId) AS HasItems,
				FTI.FailCount
	FROM		TestResults		TR
	JOIN		@FailedTestIds	FTI
	ON			TR.TestId = FTI.TestId
	JOIN		TestIndex			TI
	ON			TR.TestId = TI.TestId
	JOIN		FilePath			FP
	ON			TR.FilePathId = FP.Id
	LEFT JOIN	ExceptionMessage	EM
	ON			TR.ExceptionMessageId = EM.ExceptionId
	LEFT JOIN	ExceptionTrace		ET
	ON			TR.ExceptionTraceId = ET.ExceptionId
	WHERE		TR.BuildType = @BuildType 
	AND			TR.BuildNumber = @BuildNumber
	ORDER BY	FTI.FailCount DESC
END