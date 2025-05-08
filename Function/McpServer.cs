using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

public class GetCustomersTool
{
    public const string ToolName = "GetCustomers";
    public const string ToolDescription = "Returns a list of customers matching a filter.";
    public const string FilterName = "filter";
    public const string FilterType = "string";
    public const string FilterDescription = "$filter expression using columns like email, state, country.";

    [Function(ToolName)]
    public IEnumerable<object> Run(
        [McpToolTrigger(ToolName, ToolDescription)] ToolInvocationContext _,
        [McpToolProperty(FilterName, FilterType, FilterDescription)] string? filter)
    {
        return [new { id = 1, name = "Contoso", filter }];
    }
}

public class GetClaimsTool(ILogger<GetClaimsTool> logger)
{
    public const string ToolName = "GetClaims";
    public const string ToolDescription = "Returns a list of claims for a customer matching a filter.";
    public const string FilterName = "filter";
    public const string FilterType = "string";
    public const string FilterDescription = "$filter expression using claim_type, claim_date, etc.";

    [Function(ToolName)]
    public IEnumerable<object> Run(
        [McpToolTrigger(ToolName, ToolDescription)] ToolInvocationContext _,
        [McpToolProperty(FilterName, FilterType, FilterDescription)] string? filter)
    {
        logger.LogInformation($"Invoking {nameof(GetClaimsTool)}");
        return [new { id = 101, customer_id = 1, claim_type = "fire", filter }];
    }
}

public class GetPoliciesTool
{
    public const string ToolName = "GetPolicies";
    public const string ToolDescription = "Returns policies matching a filter.";
    public const string FilterName = "filter";
    public const string FilterType = "string";
    public const string FilterDescription = "$filter expression using type, premium, start_date, etc.";

    [Function(ToolName)]
    public IEnumerable<object> Run(
        [McpToolTrigger(ToolName, ToolDescription)] ToolInvocationContext _,
        [McpToolProperty(FilterName, FilterType, FilterDescription)] string? filter)
    {
        return [new { id = 200, customer_id = 1, type = "Auto", premium = 1234.56m, filter }];
    }
}

public class GetCommunicationHistoryTool
{
    public const string ToolName = "GetCommunicationHistory";
    public const string ToolDescription = "Returns communication records matching a filter.";
    public const string FilterName = "filter";
    public const string FilterType = "string";
    public const string FilterDescription = "$filter expression using communication_type, communication_date, etc.";

    [Function(ToolName)]
    public IEnumerable<object> Run(
        [McpToolTrigger(ToolName, ToolDescription)] ToolInvocationContext _,
        [McpToolProperty(FilterName, FilterType, FilterDescription)] string? filter)
    {
        return [new { id = 301, customer_id = 1, communication_type = "email", communication_date = "2025-01-01T12:00:00Z", filter }];
    }
}

public class GetStarDateTool
{
    public const string ToolName = "GetStarDate";
    public const string ToolDescription = "Translates the provided date into the TNG-style StarDate format.";
    public const string DateName = "date";
    public const string DateType = "datetime";
    public const string DateDescription = "The date to be converted to StarDate format.";

    [Function(ToolName)]
    public string Run(
        [McpToolTrigger(ToolName, ToolDescription)] ToolInvocationContext _,
        [McpToolProperty(DateName, DateType, DateDescription)] DateTime date)
    {
        DateTime baseDate = new(2323, 1, 1);
        double stardate = (date - baseDate).TotalDays * 1000.0 / 365.25;
        return stardate.ToString("F1");
    }
}
