-- table columns: key, time , value

USE [test-db]
GO

Create Table dbo.KafkaMessages (
	[Id] bigint NOT NULL IDENTITY (1, 1),
	[Key] nchar(10) NOT NULL,
	[Time] datetime NOT NULL,
	[Value] numeric(18, 6) NOT NULL
);
GO

ALTER TABLE dbo.KafkaMessages ADD CONSTRAINT PK_KafkaMessages_Id PRIMARY KEY CLUSTERED (Id);
GO
