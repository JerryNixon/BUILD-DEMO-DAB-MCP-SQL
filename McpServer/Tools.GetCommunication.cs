using Microsoft.DataApiBuilder.Rest;
using Microsoft.DataApiBuilder.Rest.Options;

using ModelContextProtocol.Server;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public static partial class Tools
{
    [McpServerTool, Description("""
    Search through Customer Communication History by Topic/Subject.
    This stored procedure uses vector search for natural language searches. 
    Columns:
      [Key] id (int): internal communication id
      date (DateTime): communication date
      communication_type (string): type of communication (email, call, etc)
      details (string): notes or transcript
    """)]
    public static CommunicationHistory[] GetCommunicationHistory(
        [Description("Customer Id to filter search")] int customerId,
        [Description("A string to search for comm subjects, use 'all' to return entire table.")] string subject,
        CancellationToken? cancellationToken = null)
    {
        var repo = new ProcedureRepository<CommunicationHistory>(new(string.Format(DAB_URL, "GetCommunicationHistory")));
        var options = new ProcedureOptions
        {
            Parameters = new Dictionary<string, string>
            {
                { "customerId", customerId.ToString() },
                { "subject", subject },
            }
        };

        var result = repo.ExecuteAsync(options, cancellationToken ?? CancellationToken.None).GetAwaiter().GetResult();
        return result.Result;
    }
}

public record CommunicationHistory(
    [property: Key]
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("data")] DateTime CommunicationDate,
    [property: JsonPropertyName("communication_type")] string CommunicationType,
    [property: JsonPropertyName("details")] string? Details
);