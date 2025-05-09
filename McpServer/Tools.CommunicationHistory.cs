using Microsoft.DataApiBuilder.Rest;

using ModelContextProtocol.Server;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public static partial class Tools
{
    [McpServerTool, Description("""
    Query the database to find communication history records for customers.
    Columns:
      [Key] id (int): unique communication ID
      [ForeignKey: to customers.id] customer_id (int): customer ID
      communication_type (varchar(100)): type of communication (email, phone, etc)
      communication_date (datetime2): date of communication
      details (nvarchar(max)): content of the communication
      embedding (vector(1536)): AI-generated embedding for the communication
    """)]
    public static CommunicationHistoryTable[] GetCommunicationHistoryTable(
        [Description("OData $filter string, e.g., communication_type eq 'email' or empty string to get all records")] string filter,
        CancellationToken? cancellationToken = null) =>
        GetFiltered<CommunicationHistoryTable>("CommunicationHistory", filter, cancellationToken);

    [McpServerTool, Description("Upsert a CommunicationHistoryTable record; returns updated record.")]
    public static CommunicationHistoryTable UpsertCommunicationHistoryTable(
        [Description("Communication ID (int)")] int id,
        [Description("Customer ID (int)")] int customer_id,
        [Description("Communication type (varchar(100))")] string communication_type,
        [Description("Communication date (datetime2)")] DateTime communication_date,
        [Description("Details of the communication (nvarchar(max))")] string details)
    {
        var record = new CommunicationHistoryTable(
            Id: id,
            CustomerId: customer_id,
            CommunicationType: communication_type,
            CommunicationDate: communication_date,
            Details: details
        );

        var repo = new TableRepository<CommunicationHistoryTable>(new(string.Format(DAB_URL, "CommunicationHistory")));
        var result = repo.PatchAsync(record, default, CancellationToken.None).GetAwaiter().GetResult();
        return result.Result;
    }
}

public record CommunicationHistoryTable(
   [property: Key]
   [property: JsonPropertyName("id")] int Id,
   [property: JsonPropertyName("customer_id")] int CustomerId,
   [property: JsonPropertyName("communication_type")] string CommunicationType,
   [property: JsonPropertyName("communication_date")] DateTime CommunicationDate,
   [property: JsonPropertyName("details")] string Details
);