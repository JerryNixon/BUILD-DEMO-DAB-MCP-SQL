using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

public record Claim(
    [property: Key]
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("claim_date")] DateTime ClaimDate,
    [property: JsonPropertyName("claim_type")] string ClaimType,
    [property: JsonPropertyName("customer_id")] int CustomerId,
    [property: JsonPropertyName("details")] string? Details
);

public record CommunicationHistory(
    [property: Key]
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("data")] DateTime CommunicationDate,
    [property: JsonPropertyName("communication_type")] string CommunicationType,
    [property: JsonPropertyName("details")] string? Details
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
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("details")] string? Details
);

public record Policy(
    [property: Key]
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("start_date")] DateTime StartDate,
    [property: JsonPropertyName("duration")] int Duration,
    [property: JsonPropertyName("premium")] decimal Premium,
    [property: JsonPropertyName("payment_type")] string PaymentType,
    [property: JsonPropertyName("payment_amount")] decimal PaymentAmount,
    [property: JsonPropertyName("customer_id")] int CustomerId,
    [property: JsonPropertyName("additional_notes")] string? AdditionalNotes
);