# SocialTracker-Jobs
Azure functions to calculate the difference in social follower count over time


## SQL Schema
SQL Server is used as the database. This is the script that was used to create the tables:

``` sql
-- Accounts data store
IF OBJECT_ID('dbo.UAccounts', 'U') IS NOT NULL
DROP TABLE dbo.UAccounts
GO

CREATE TABLE dbo.UAccounts
(
    [AccID] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT(newid()),
    [Network] NVARCHAR(15) NOT NULL,
    [Username] NVARCHAR(max) NOT NULL,
    [Added] DATETIME NOT NULL DEFAULT(getutcdate()),
    [Removed] BIT NOT NULL DEFAULT(0)
); CREATE INDEX IX_UAccounts_Network ON dbo.UAccounts ([Network])
GO

-- Follower count data store
IF OBJECT_ID('dbo.UData', 'U') IS NOT NULL
DROP TABLE dbo.UData
GO

CREATE TABLE dbo.UData
(
    [AccID] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY FOREIGN KEY REFERENCES UAccounts (AccID),
    [Run] DATETIME NOT NULL DEFAULT(getutcdate()),
    [FCount] INT NOT NULL
); CREATE INDEX IX_UData_AccID ON dbo.UData ([AccID])
GO
```