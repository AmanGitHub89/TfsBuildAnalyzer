USE [TfsBuildAnalyzer]
GO

IF EXISTS (select 1 from sys.procedures where name='sp_InsertResults')
BEGIN
	DROP PROCEDURE	sp_InsertResults
END
GO

IF EXISTS (select 1 from sys.procedures where name='sp_GetBacklogItemsForTestList')
BEGIN
	DROP PROCEDURE	sp_GetBacklogItemsForTestList
END
GO

IF EXISTS (select 1 from sysobjects where name='TestResults' and xtype='U')
BEGIN
	DROP TABLE  TestResults
END

IF EXISTS (select 1 from sysobjects where name='BuildInfo' and xtype='U')
BEGIN
	DROP TABLE  BuildInfo
END

IF EXISTS (select 1 from sysobjects where name='BuildTypes' and xtype='U')
BEGIN
	DROP TABLE  BuildTypes
END

IF EXISTS (select 1 from sysobjects where name='FilePath' and xtype='U')
BEGIN
	DROP TABLE  FilePath
END

IF EXISTS (select 1 from sysobjects where name='ExceptionMessage' and xtype='U')
BEGIN
	DROP TABLE  ExceptionMessage
END

IF EXISTS (select 1 from sysobjects where name='ExceptionTrace' and xtype='U')
BEGIN
	DROP TABLE  ExceptionTrace
END

IF EXISTS (select 1 from sysobjects where name='BacklogItems' and xtype='U')
BEGIN
	DROP TABLE  BacklogItems
END

IF EXISTS (select 1 from sysobjects where name='TestIndex' and xtype='U')
BEGIN
	DROP TABLE  TestIndex
END

IF EXISTS (select 1 from sysobjects where name='AppFeedback' and xtype='U')
BEGIN
	DROP TABLE  AppFeedback
END

IF EXISTS (select 1 from sysobjects where name='AppIssues' and xtype='U')
BEGIN
	DROP TABLE  AppIssues
END

IF EXISTS (select 1 from sys.types where name='TestResult')
BEGIN
	DROP TYPE  TestResult
END

IF EXISTS (select 1 from sys.types where name='TestList')
BEGIN
	DROP TYPE  TestList
END


IF NOT EXISTS(SELECT 1 FROM sysobjects WHERE NAME='BuildTypes' and xtype='U')
BEGIN
CREATE TABLE BuildTypes
(
	Id				INTEGER IDENTITY(1,1)			NOT NULL,
	BuildType		VARCHAR(100)			UNIQUE	NOT NULL,
	CONSTRAINT [PK_BuildTypes] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)	ON [PRIMARY]
END
GO

IF NOT EXISTS(SELECT 1 FROM sysobjects WHERE NAME='BuildInfo' and xtype='U')
BEGIN
CREATE TABLE BuildInfo
(
	BuildType		VARCHAR(100)	NOT NULL,
	BuildNumber		VARCHAR(100)	NOT NULL,
	BuildStatus		VARCHAR(30)		NOT NULL,
	BuildUrl		VARCHAR(1000)	NOT NULL,
	BuildDate		DATETIME		NOT NULL,
	PassedCount		INTEGER			NOT NULL,
	FailedCount		INTEGER			NOT NULL,
	IgnoredCount	INTEGER			NOT NULL,
	CONSTRAINT [PK_BuildInfo] PRIMARY KEY 
	(
		BuildType, BuildNumber
	)	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)	ON [PRIMARY]
END
GO


IF NOT EXISTS(SELECT * FROM sysobjects WHERE NAME='ExceptionMessage' and xtype='U')
BEGIN
CREATE TABLE ExceptionMessage
(
	ExceptionId			INTEGER IDENTITY(1,1)	NOT NULL,
	ExceptionMessage	NVARCHAR(MAX)			NOT NULL,
	CONSTRAINT [PK_ExceptionMessage] PRIMARY KEY CLUSTERED 
	(
		[ExceptionId] ASC
	)	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)	ON [PRIMARY]
END
GO

IF NOT EXISTS(SELECT * FROM sysobjects WHERE NAME='ExceptionTrace' and xtype='U')
BEGIN
CREATE TABLE ExceptionTrace
(
	ExceptionId			INTEGER IDENTITY(1,1)	NOT NULL,
	ExceptionTrace		NVARCHAR(MAX)			NOT NULL,
	CONSTRAINT [PK_ExceptionTrace] PRIMARY KEY CLUSTERED 
	(
		[ExceptionId] ASC
	)	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)	ON [PRIMARY]
END
GO

IF NOT EXISTS(SELECT * FROM sysobjects WHERE NAME='TestIndex' and xtype='U')
BEGIN
CREATE TABLE TestIndex
(
	TestId			UNIQUEIDENTIFIER		NOT NULL,
	TestName		NVARCHAR(MAX)			NOT NULL,
	CONSTRAINT [PK_TestIndex] PRIMARY KEY CLUSTERED 
	(
		[TestId] ASC
	)	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)	ON [PRIMARY]
END
GO

IF NOT EXISTS(SELECT * FROM sysobjects WHERE NAME='FilePath' and xtype='U')
BEGIN
CREATE TABLE FilePath
(
	Id			INTEGER IDENTITY(1,1)	NOT NULL,
	FilePath	NVARCHAR(MAX)			NOT NULL,
	CONSTRAINT [PK_FilePath] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)	ON [PRIMARY]
END
GO

IF NOT EXISTS(SELECT * FROM sysobjects WHERE NAME='BacklogItems' and xtype='U')
BEGIN
CREATE TABLE BacklogItems
(
	BacklogId		INTEGER				NOT NULL,
	TestId			UNIQUEIDENTIFIER	NOT NULL,
	BacklogTitle	NVARCHAR(MAX)		NOT NULL,
	CONSTRAINT FK_BacklogItems_TestIndex FOREIGN KEY (TestId) REFERENCES TestIndex(TestId),
	CONSTRAINT [PK_BacklogItems] PRIMARY KEY CLUSTERED 
	(
		BackLogId, TestId
	)	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)	ON [PRIMARY]
END
GO

IF NOT EXISTS(SELECT * FROM sysobjects WHERE NAME='TestResults' and xtype='U')
BEGIN
CREATE TABLE TestResults
(
	TestResultId		INTEGER	IDENTITY(1,1)	NOT NULL,
	BuildType			VARCHAR(100)			NOT NULL,
	BuildNumber			VARCHAR(100)			NOT NULL,
	TestId				UNIQUEIDENTIFIER		NOT NULL,
	TestAgent			VARCHAR(100)			NOT NULL,
	ExceptionMessageId	INTEGER					NULL,
	ExceptionTraceId	INTEGER					NULL,
	TestStatus			VARCHAR(10)				NOT NULL,
	FilePathId			INTEGER					NOT NULL,
	Duration			VARCHAR(20)				NULL,
	CONSTRAINT FK_TestResults_TestIndex FOREIGN KEY (TestId) REFERENCES TestIndex(TestId),
	CONSTRAINT FK_TestResults_BuildInfo FOREIGN KEY (BuildType, BuildNumber) REFERENCES BuildInfo(BuildType, BuildNumber),
	CONSTRAINT FK_TestResults_ExceptionMessage FOREIGN KEY (ExceptionMessageId) REFERENCES ExceptionMessage(ExceptionId),
	CONSTRAINT FK_TestResults_ExceptionTrace FOREIGN KEY (ExceptionTraceId) REFERENCES ExceptionTrace(ExceptionId),
	CONSTRAINT FK_TestResults_FilePath FOREIGN KEY (FilePathId) REFERENCES FilePath(Id),
	CONSTRAINT [PK_TestResults] PRIMARY KEY CLUSTERED 
		(
			[TestResultId] ASC
		)	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
	)	ON [PRIMARY]
END
GO

IF NOT EXISTS (select 1 from sys.types where name='TestResult')
BEGIN
	CREATE TYPE TestResult AS TABLE
	(
		TestId				UNIQUEIDENTIFIER	NOT NULL,
		TestName			NVARCHAR(MAX)		NOT NULL,
		TestAgent			VARCHAR(100)		NOT NULL,
		ExceptionMessage	NVARCHAR(MAX)		NULL,
		ExceptionTrace		NVARCHAR(MAX)		NULL,
		TestStatus			VARCHAR(10)			NOT NULL,
		FilePath			NVARCHAR(MAX)		NOT NULL,
		Duration			VARCHAR(20)			NULL
	)
END
GO

IF NOT EXISTS (select 1 from sys.types where name='TestList')
BEGIN
	CREATE TYPE TestList AS TABLE
	(
		TestId			UNIQUEIDENTIFIER	NOT NULL
	)
END
GO

IF NOT EXISTS(SELECT * FROM sysobjects WHERE NAME='AppFeedback' and xtype='U')
BEGIN
CREATE TABLE AppFeedback
(
	FeedbackId				UNIQUEIDENTIFIER		NOT NULL,
	FeedbackDescription		NVARCHAR(MAX)			NOT NULL,
	FeedbackUser			VARCHAR(200)			NULL,
	CONSTRAINT [PK_AppFeedback] PRIMARY KEY CLUSTERED 
	(
		[FeedbackId] ASC
	)	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)	ON [PRIMARY]
END
GO

IF NOT EXISTS(SELECT * FROM sysobjects WHERE NAME='AppIssues' and xtype='U')
BEGIN
CREATE TABLE AppIssues
(
	IssueId				UNIQUEIDENTIFIER		NOT NULL,
	IssueDescription	NVARCHAR(MAX)			NOT NULL,
	IssueUser			VARCHAR(200)			NULL,
	CONSTRAINT [PK_AppIssues] PRIMARY KEY CLUSTERED 
	(
		[IssueId] ASC
	)	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)	ON [PRIMARY]
END
GO
