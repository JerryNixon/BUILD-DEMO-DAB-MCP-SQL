using Microsoft.DataApiBuilder.Rest;
using Microsoft.DataApiBuilder.Rest.Options;

using ModelContextProtocol.Server;

using System.ComponentModel;

[McpServerToolType]
public static class Tools
{
    private const string DAB_URL = "https://salmon-meadow-0d9fbce0f.6.azurestaticapps.net/data-api/api/{0}";

    [McpServerTool, Description("""
    Says Hello to a user to show the code is working.
    """)]
    public static string Echo(string username) => "Hello " + username + ". Your code is working!";

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
        [Description("OData $filter string, e.g., claim_type eq 'auto'")] string filter,
        CancellationToken? cancellationToken = null) =>
        GetFiltered<Claim>("claims", filter, cancellationToken);

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
        [Description("OData $filter string, e.g., type eq 'home'")] string filter,
        CancellationToken? cancellationToken = null) =>
        GetFiltered<Policy>("policies", filter, cancellationToken);

    [McpServerTool, Description("""
    Return interactions history for a customer based on customer id and subject.
    Columns:
      [Key] id (int): internal communication id
      [ForeignKey: to customers.id] customer_id (int): customer id
      communication_date (DateTime): communication date
      communication_type (string): type of communication (email, call, etc)
      details (string): notes or transcript
    """)]
    public static CommunicationHistory[] GetCommunicationHistory(
        [Description("OData $filter string, e.g., communication_type eq 'email'")] string filter,
        CancellationToken? cancellationToken = null) =>
        GetFiltered<CommunicationHistory>("communication_history", filter, cancellationToken);

    [McpServerTool, Description("""
    Query the database to find customer data.
    Columns:
      [Key] id (int): internal customer id
      first_name (string): customer's first name
      last_name (string): customer's last name
      email (string): email address
      address (string): street address
      city (string): city of residence
      state (string): state or province
      zip (string): zip/postal code
      country (string): country
      details (string): additional info
      active_policies (string): other types of policies (life, car, etc)
    """)]
    public static Customer[] GetCustomers(
        [Description("OData $filter string, e.g., last_name eq 'Smith'")] string filter,
        CancellationToken? cancellationToken = null) =>
        GetFiltered<Customer>("customers", filter, cancellationToken);


    private static T[] GetFiltered<T>(string path, string filter, CancellationToken? cancellationToken = null) where T : class
    {
        var repo = new TableRepository<T>(new(string.Format(DAB_URL, path)));
        var options = new TableOptions { Filter = filter };
        var result = repo.GetAsync(options, cancellationToken ?? CancellationToken.None).GetAwaiter().GetResult();
        return result.Result;
    }
}
