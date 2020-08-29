CREATE TABLE [dbo].Users
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [UserID] INT NOT NULL, 
    [FirstName] NCHAR(50) NOT NULL, 
    [LastName] NVARCHAR(50) NOT NULL, 
    [EmailID] NCHAR(254) NOT NULL, 
    [DateOfBirth] DATETIME NULL, 
    [Password] NVARCHAR(MAX) NOT NULL, 
    [IsEmailVerified] BIT NOT NULL, 
    [ActivationCode] UNIQUEIDENTIFIER NOT NULL
)
