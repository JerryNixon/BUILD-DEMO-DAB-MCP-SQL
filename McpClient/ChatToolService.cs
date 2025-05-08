using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using Azure.AI.OpenAI;
using Azure;

public class AiService
{
    private readonly string azureOpenAiEndpoint;
    private readonly string openAiKey;
    private readonly string deploymentName;
    private readonly string toolsEndpoint;

    public List<ChatMessage> Messages = [];

    public AiService(IConfiguration config, out IChatClient client, out IList<McpClientTool> tools)
    {
        azureOpenAiEndpoint = config["AZURE_OPENAI_ENDPOINT"] ?? throw new InvalidOperationException("Missing AZURE_OPENAI_ENDPOINT");
        openAiKey = config["AZURE_OPENAI_KEY"] ?? throw new InvalidOperationException("Missing OPENAI_API_KEY");
        deploymentName = config["AZURE_OPENAI_DEPLOYMENT"] ?? "gpt-4o";
        toolsEndpoint = config["TOOLS_ENDPOINT"] ?? throw new InvalidOperationException("Missing TOOLS_ENDPOINT");

        client = GetChatClient();
        tools = GetMcpTools();
    }

    public IChatClient GetChatClient()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddOpenTelemetry(opt => opt.AddOtlpExporter()));

        var openAiClient = new AzureOpenAIClient(new Uri(azureOpenAiEndpoint), new AzureKeyCredential(openAiKey));
        var chatClient = openAiClient.GetChatClient(deploymentName);

        return chatClient.AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .UseOpenTelemetry(loggerFactory: loggerFactory, configure: o => o.EnableSensitiveData = true)
            .Build();
    }

    public IList<McpClientTool> GetMcpTools()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddOpenTelemetry(opt => opt.AddOtlpExporter()));

        var openAiClient = new AzureOpenAIClient(new Uri(azureOpenAiEndpoint), new AzureKeyCredential(openAiKey));
        var chatClient = openAiClient.GetChatClient(deploymentName);

        using IChatClient samplingClient = chatClient.AsIChatClient()
            .AsBuilder()
            .UseOpenTelemetry(loggerFactory: loggerFactory, configure: o => o.EnableSensitiveData = true)
            .Build();

        var mcpClient = McpClientFactory.CreateAsync(
            new SseClientTransport(new()
            {
                Endpoint = new Uri(toolsEndpoint),
                Name = "MyMcpServer"
            }),
            clientOptions: new()
            {
                Capabilities = new()
                {
                    Sampling = new()
                    {
                        SamplingHandler = samplingClient.CreateSamplingHandler()
                    }
                }
            },
            loggerFactory: loggerFactory
        );
        var client = mcpClient.GetAwaiter().GetResult();
        return client.ListToolsAsync().GetAwaiter().GetResult();
    }
}
