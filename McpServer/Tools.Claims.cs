using Microsoft.DataApiBuilder.Rest;

using ModelContextProtocol.Server;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public static partial class Tools
{
    [McpServerTool, Description("""
    Query the database to find claims data for customers.
    Columns:
      [Key] id (int): internal claim id
      [ForeignKey: to customers.id] customer_id (int): customer id
      claim_type (string): claim type (auto, home, life, health, etc)
      claim_date (DateTime): claim date
      details (string): details and notes about the claim added by the agent
    """)]
    public static Claim[] GetClaims(
        [Description("OData $filter string, e.g., claim_type eq 'auto' or empty string to get all records")] string filter,
        CancellationToken? cancellationToken = null) =>
        GetFiltered<Claim>("Claim", filter, cancellationToken);

    [McpServerTool, Description("Upsert a Claim record; returns updated Claim.")]
    public static Claim UpsertClaim(
    [Description("Claim ID (int)")] int id,
    [Description("Customer ID (int)")] int customer_id,
    [Description("Claim type (nvarchar(100))")] string claim_type,
    [Description("Claim date (datetime)")] DateTime claim_date,
    [Description("Details and notes about the claim (nvarchar(max))")] string? details)
    {
        var claim = new Claim(
            Id: id,
            ClaimDate: claim_date,
            ClaimType: claim_type,
            CustomerId: customer_id,
            Details: details
        );

        var repo = new TableRepository<Claim>(new(string.Format(DAB_URL, "Claim")));
        var result = repo.PatchAsync(claim, default, CancellationToken.None).GetAwaiter().GetResult();
        return result.Result;
    }
}

public record Claim(
    [property: Key]
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("claim_date")] DateTime ClaimDate,
    [property: JsonPropertyName("claim_type")] string ClaimType,
    [property: JsonPropertyName("customer_id")] int CustomerId,
    [property: JsonPropertyName("details")] string? Details
);