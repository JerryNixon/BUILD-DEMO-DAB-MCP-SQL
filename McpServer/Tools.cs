using Microsoft.DataApiBuilder.Rest;
using Microsoft.DataApiBuilder.Rest.Options;

using ModelContextProtocol.Server;

using System.ComponentModel;
using System.IO;

[McpServerToolType]
public static class Tools
{
    // private const string DAB_URL = "https://salmon-meadow-0d9fbce0f.6.azurestaticapps.net/data-api/api/{0}";
    private const string DAB_URL = "http://localhost:5000/api/{0}";

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
        [Description("OData $filter string, e.g., claim_type eq 'auto' or empty string to get all records")] string filter,
        CancellationToken? cancellationToken = null) =>
        GetFiltered<Claim>("Claim", filter, cancellationToken);

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

    [McpServerTool, Description("""
    Search interactions history for a customer based on customer id and subject.
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
        [Description("OData $filter string, e.g., last_name eq 'Smith' or empty string to get all records.")] string filter,
        CancellationToken? cancellationToken = null) =>
        GetFiltered<Customer>("Customer", filter, cancellationToken);

    [McpServerTool, Description("Update a Customer record; resturns updated Customer.")]
    public static Customer UpdateCustomer(
       [Description("Customer ID (int)")] int id,
       [Description("First name (nvarchar(100))")] string first_name,
       [Description("Last name (nvarchar(100))")] string last_name,
       [Description("Email address (nvarchar(100))")] string email,
       [Description("Street address (nvarchar(100))")] string address,
       [Description("City (nvarchar(100))")] string city,
       [Description("State or province (nvarchar(100))")] string state,
       [Description("Postal code (nvarchar(100))")] string zip,
       [Description("Country (nvarchar(100))")] string country)
    {
        var customer = new Customer(
            Id: id,
            FirstName: first_name,
            LastName: last_name,
            Email: email,
            Address: address,
            City: city,
            State: state,
            Zip: zip,
            Country: country
            );
        var repo = new TableRepository<Customer>(new(string.Format(DAB_URL, "Customer")));
        var result = repo.PatchAsync(customer, default, CancellationToken.None).GetAwaiter().GetResult();
        return result.Result;
    }

    private static T[] GetFiltered<T>(string path, string filter, CancellationToken? cancellationToken = null) where T : class
    {
        var repo = new TableRepository<T>(new(string.Format(DAB_URL, path)));
        var options = string.IsNullOrWhiteSpace(filter) ? default : new TableOptions { Filter = filter };
        var result = repo.GetAsync(options, cancellationToken ?? CancellationToken.None).GetAwaiter().GetResult();
        return result.Result;
    }
}
