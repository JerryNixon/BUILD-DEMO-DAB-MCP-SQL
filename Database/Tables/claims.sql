CREATE TABLE [dbo].[claims]
(
    [id] INT NOT NULL,
    [customer_id] int NOT NULL,
    [claim_type] VARCHAR (100) NOT NULL,
    [claim_date] DATETIME2(0) NOT NULL,
    [details] NVARCHAR(MAX) NOT NULL,

    PRIMARY KEY NONCLUSTERED ([id] ASC)
)