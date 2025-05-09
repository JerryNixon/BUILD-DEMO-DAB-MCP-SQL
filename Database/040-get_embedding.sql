create or alter procedure [dbo].[get_embedding]
@inputText nvarchar(max),
@embedding vector(1536) output
as
begin try
    declare @retval int;
    declare @payload nvarchar(max) = json_object('input': @inputText);
    declare @response nvarchar(max)
    exec @retval = sp_invoke_external_rest_endpoint
        @url = '$OPENAI_URL$/openai/deployments/$OPENAI_EMBEDDING_DEPLOYMENT_NAME$/embeddings?api-version=2023-03-15-preview',
        @method = 'POST',
        @credential = [$OPENAI_URL$],
        @payload = @payload,
        @response = @response output;
end try
begin catch
    select 
        'SQL' as error_source, 
        error_number() as error_code,
        error_message() as error_message
    return;
end catch

if (@retval != 0) begin
    select 
        'OPENAI' as error_source, 
        json_value(@response, '$.result.error.code') as error_code,
        json_value(@response, '$.result.error.message') as error_message,
        @response as error_response
    return;
end;

declare @re nvarchar(max) = json_query(@response, '$.result.data[0].embedding')
set @embedding = cast(@re as vector(1536));

return @retval
go;

CREATE OR ALTER PROCEDURE [dbo].[process_missing_embeddings]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @id INT;
    DECLARE @text NVARCHAR(MAX);
    DECLARE @embedding VECTOR(1536);

    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT id, details
        FROM communication_history
        WHERE embedding IS NULL;

    OPEN cur;
    FETCH NEXT FROM cur INTO @id, @text;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            EXEC dbo.get_embedding @text, @embedding OUTPUT;

            UPDATE communication_history
            SET embedding = @embedding
            WHERE id = @id;
        END TRY
        BEGIN CATCH
            -- optional: log error or skip
            PRINT CONCAT('Failed to process id ', @id, ': ', ERROR_MESSAGE());
        END CATCH

        FETCH NEXT FROM cur INTO @id, @text;
    END

    CLOSE cur;
    DEALLOCATE cur;
END
GO
