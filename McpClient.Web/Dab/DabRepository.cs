using Microsoft.DataApiBuilder.Rest;
using Microsoft.DataApiBuilder.Rest.Options;

namespace McpClient.Web.Dab;

public class DabRepository
{
    private readonly string dabUrl;

    public DabRepository(IConfiguration config)
    {
        dabUrl = config["DAB_ENDPOINT"] ?? throw new InvalidOperationException("Missing DAB_ENDPOINT");
        if (!dabUrl.EndsWith("/"))
        {
            dabUrl += "/";
        }
    }

    public Task<IEnumerable<Customer>> GetCustomersAsync() =>
        GetAsync<Customer>("Customer");

    public Task<IEnumerable<Policy>> GetPoliciesByCustomerIdAsync(int customerId) =>
        GetAsync<Policy>("Policy", $"customer_id eq {customerId}");

    public Task<IEnumerable<CommunicationHistoryTable>> GetCommunicationsByCustomerIdAsync(int customerId) =>
        GetAsync<CommunicationHistoryTable>("CommunicationHistory", $"customer_id eq {customerId}");

    public Task<IEnumerable<Claim>> GetClaimsByCustomerIdAsync(int customerId) =>
        GetAsync<Claim>("Claim", $"customer_id eq {customerId}");

    private async Task<IEnumerable<T>> GetAsync<T>(string name, string? filter = null) where T : class
    {
        var url = new Uri(dabUrl + name);
        var repository = new TableRepository<T>(url);
        if (filter is null)
        {
            var response = await repository.GetAsync();
            return response.Result;
        }
        else
        {
            var option = new TableOptions { Filter = filter };
            var response = await repository.GetAsync(option);
            return response.Result;
        }
    }
}
