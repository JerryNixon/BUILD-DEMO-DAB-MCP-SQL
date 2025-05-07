CREATE TABLE [dbo].[policies]
(
    [id] INT NOT NULL,
    [customer_id] int NOT NULL,
    [type] VARCHAR (100) NOT NULL,
    [premium] DECIMAL(9,4) NOT NULL,
    [payment_type] VARCHAR(50) NOT NULL,
    [start_date] DATE NOT NULL, 
    [duration] VARCHAR(50) NOT NULL,
    [payment_amount] DECIMAL(9,4) NOT NULL,
    [additional_notes] NVARCHAR(MAX) NULL,

    PRIMARY KEY NONCLUSTERED ([id] ASC)
)