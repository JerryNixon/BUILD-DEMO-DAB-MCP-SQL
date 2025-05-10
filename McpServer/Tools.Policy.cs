using Microsoft.DataApiBuilder.Rest;

using ModelContextProtocol.Server;

using System.ComponentModel;

public static partial class Tools
{
    [McpServerTool, Description("""
    Query the database to find policies data for customers.
    Columns:
        [Key] id (int): internal policy id
        [ForeignKey: to customers.id] customer_id (int): customer id
        type (string): policy type (auto, home, life, health, etc)
        premium (decimal): premium for the policy duration
        payment_type (string): payment type (monthly, yearly, etc)
        start_date (DateTime): policy start date
        duration (int): policy duration in months
        payment_amount (decimal): payment amount
        additional_notes (string): details and notes about the policy and payment status
    """)]
    public static Policy[] GetPolicies(
        [Description("OData $filter string, e.g., type eq 'home' or empty string to get all records")] string filter,
        CancellationToken? cancellationToken = null) =>
        GetFiltered<Policy>("Policy", filter, cancellationToken);

    [McpServerTool, Description("Upsert a Policy record; returns updated Policy.")]
    public static Policy UpsertPolicy(
    [Description("Policy ID (int)")] int id,
    [Description("Customer ID (int)")] int customer_id,
    [Description("Policy type (nvarchar(100))")] string type,
    [Description("Premium amount (decimal)")] decimal premium,
    [Description("Payment type (nvarchar(100))")] string payment_type,
    [Description("Policy start date (datetime) The text should be in format 'yyyy-MM-ddTHH:mm:sszzz' and each field value is within valid range.")] DateTime start_date,
    [Description("Policy duration (nvarchar(100))")] string duration,
    [Description("Payment amount (decimal)")] decimal payment_amount,
    [Description("Notes about the policy (nvarchar(1000))")] string? additional_notes)
    {
        var policy = new Policy(
            Id: id,
            CustomerId: customer_id,
            Type: type,
            Premium: premium,
            PaymentType: payment_type,
            StartDate: start_date,
            Duration: duration,
            PaymentAmount: payment_amount,
            AdditionalNotes: additional_notes
        );

        var repo = new TableRepository<Policy>(new(string.Format(DAB_URL, "Policy")));
        var result = repo.PatchAsync(policy, default, CancellationToken.None).GetAwaiter().GetResult();
        return result.Result;
    }
}
