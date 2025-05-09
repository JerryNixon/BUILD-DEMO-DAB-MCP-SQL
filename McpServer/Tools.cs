using Microsoft.DataApiBuilder.Rest;
using Microsoft.DataApiBuilder.Rest.Options;

using ModelContextProtocol.Server;

using System.ComponentModel;

[McpServerToolType]
public static partial class Tools
{
    // private const string DAB_URL = "https://salmon-meadow-0d9fbce0f.6.azurestaticapps.net/data-api/api/{0}";
    private const string DAB_URL = "http://localhost:5000/api/{0}";

    [McpServerTool, Description("Says Hello to a user to show the code is working. Answers 'is this working?' amung others.")]
    public static string Echo(string username) => "Hello " + username + ". Your code is working!";

    private static T[] GetFiltered<T>(string path, string filter, CancellationToken? cancellationToken = null) where T : class
    {
        var repo = new TableRepository<T>(new(string.Format(DAB_URL, path)));
        var options = string.IsNullOrWhiteSpace(filter) ? default : new TableOptions { Filter = filter };
        var result = repo.GetAsync(options, cancellationToken ?? CancellationToken.None).GetAwaiter().GetResult();
        return result.Result;
    }
}
