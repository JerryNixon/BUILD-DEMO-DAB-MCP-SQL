CREATE TABLE [dbo].[communication_history]
(
    [id] INT NOT NULL,
    [customer_id] int NOT NULL,
    [communication_type] VARCHAR (100) NOT NULL,
    [communication_date] DATETIME2(0) NOT NULL,
    [details] NVARCHAR(MAX) NOT NULL

    PRIMARY KEY NONCLUSTERED ([id] ASC)
);

