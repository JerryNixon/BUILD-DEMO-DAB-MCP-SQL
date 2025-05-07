CREATE TABLE [dbo].[customers]
(
    [id] INT NOT NULL,
    [first_name] NVARCHAR(100) NOT NULL,
    [last_name] NVARCHAR(100) NOT NULL,
    [address] NVARCHAR(100) NOT NULL,
    [city] NVARCHAR(100) NOT NULL,
    [state] NVARCHAR(100) NOT NULL,
    [zip] NVARCHAR(100) NOT NULL,
    [country] NVARCHAR(100) NOT NULL,
    [email] NVARCHAR(100) NOT NULL,
    [details] VARCHAR(MAX) NULL,

    PRIMARY KEY NONCLUSTERED ([id] ASC),
    UNIQUE NONCLUSTERED ([email] ASC)
)