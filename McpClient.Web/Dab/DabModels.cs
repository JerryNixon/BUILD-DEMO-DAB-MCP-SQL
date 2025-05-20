using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Client.Web.Dab;

public record Claim(
    [property: Key]
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("claim_date")] DateTime ClaimDate,
    [property: JsonPropertyName("claim_type")] string ClaimType,
    [property: JsonPropertyName("customer_id")] int CustomerId,
    [property: JsonPropertyName("details")] string? Details
);

public record CommunicationHistoryTable(
   [property: Key]
   [property: JsonPropertyName("id")] int Id,
   [property: JsonPropertyName("customer_id")] int CustomerId,
   [property: JsonPropertyName("communication_type")] string CommunicationType,
   [property: JsonPropertyName("communication_date")] DateTime CommunicationDate,
   [property: JsonPropertyName("details")] string Details
);

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

public record Policy(
    [property: Key]
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("start_date")] DateTime StartDate,
    [property: JsonPropertyName("duration")] string Duration,
    [property: JsonPropertyName("premium")] decimal Premium,
    [property: JsonPropertyName("payment_type")] string PaymentType,
    [property: JsonPropertyName("payment_amount")] decimal PaymentAmount,
    [property: JsonPropertyName("customer_id")] int CustomerId,
    [property: JsonPropertyName("additional_notes")] string? AdditionalNotes
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