using Microsoft.DataApiBuilder.Rest;

using ModelContextProtocol.Server;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

public static partial class Tools
{
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

    [McpServerTool, Description("Upsert a Customer record; returns updated Customer.")]
    public static Customer UpsertCustomer(
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
}

public record Customer(
    [property: Key]
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string LastName,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("address")] string? Address,
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("state")] string? State,
    [property: JsonPropertyName("zip")] string? Zip,
    [property: JsonPropertyName("country")] string? Country)
{
    [JsonPropertyName("details")]
    [JsonConverter(typeof(EmbeddedJsonConverter<CustomerDetails>))]
    public CustomerDetails? Details { get; init; }
}

public record CustomerDetails(
    [property: JsonPropertyName("active-policies")] List<string> ActivePolicies
);

public class EmbeddedJsonConverter<T> : JsonConverter<T?>
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var json = doc.RootElement.GetRawText();
            return JsonSerializer.Deserialize<T>(json, options);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString();
            return raw is not null ? JsonSerializer.Deserialize<T>(raw, options) : default;
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        var json = JsonSerializer.Serialize(value, options);
        writer.WriteRawValue(json); // emit as inline object
    }
}