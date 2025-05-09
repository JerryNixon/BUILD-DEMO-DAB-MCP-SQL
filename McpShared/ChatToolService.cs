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
using System.Text;

public class AiService
{
    private readonly string azureOpenAiEndpoint;
    private readonly string openAiKey;
    private readonly string deploymentName;
    private readonly string toolsEndpoint;

    private AzureOpenAIClient openAiClient = default!;
    private OpenAI.Chat.ChatClient chatClient = default!;
    private IChatClient client = default!;
    private readonly List<McpClientTool> tools = [];

    private ILoggerFactory loggerFactory = default!;
    private ILogger<AiService> logger = default!;
    public readonly List<ChatMessage> Messages = [];
    private ChatOptions chatOptions = default!;

    public AiService(IConfiguration config)
    {
        azureOpenAiEndpoint = config["AZURE_OPENAI_ENDPOINT"] ?? throw new InvalidOperationException("Missing AZURE_OPENAI_ENDPOINT");
        openAiKey = config["AZURE_OPENAI_KEY"] ?? throw new InvalidOperationException("Missing AZURE_OPENAI_KEY");
        deploymentName = config["AZURE_OPENAI_DEPLOYMENT"] ?? throw new InvalidOperationException("Missing AZURE_OPENAI_DEPLOYMENT");
        toolsEndpoint = config["TOOLS_ENDPOINT"] ?? throw new InvalidOperationException("Missing TOOLS_ENDPOINT");

        loggerFactory = LoggerFactory.Create(builder =>
            builder.AddOpenTelemetry(opt => opt.AddOtlpExporter()));

        logger = loggerFactory.CreateLogger<AiService>();

        logger.LogInformation("AI service constructed with endpoint: {Endpoint} and tools: {Tools}", azureOpenAiEndpoint, toolsEndpoint);
    }

    public async Task<AiService> InitAsync(string? systemPrompt = null)
    {
        logger.LogInformation("Initializing AI service with system prompt: {SystemPrompt}", systemPrompt ?? "null");

        openAiClient = new AzureOpenAIClient(
            endpoint: new Uri(azureOpenAiEndpoint),
            credential: new AzureKeyCredential(openAiKey));

        chatClient = openAiClient.GetChatClient(deploymentName);

        client = GetChatClient(systemPrompt);

        tools.AddRange(await GetMcpToolsAsync());

        return this;
    }

    public async Task<string> ChatAsync(params string[] input)
    {
        logger.LogInformation("Chatting with AI service. Input: {Input}", string.Join(", ", input));

        if (client is null)
        {
            logger.LogError("AI service not initialized. Call InitAsync() first.");
            throw new InvalidOperationException("AI service not initialized. Call InitAsync() first.");
        }

        foreach (var i in input)
        {
            Messages.Add(new(ChatRole.User, i));
        }

        var updates = client.GetStreamingResponseAsync(
            messages: Messages,
            options: chatOptions ??= new() { Tools = [.. tools] });

        var result = new StringBuilder();

        await foreach (var update in updates)
        {
            result.Append(update.ToString());
            Messages.AddMessages(update);
        }

        return result.ToString();
    }

    public IChatClient GetChatClient(string? systemPrompt)
    {
        logger.LogInformation("Creating chat client with system prompt: {SystemPrompt}", systemPrompt ?? "null");

        if (chatClient is null)
        {
            logger.LogError("AI service not initialized. Call InitAsync() first.");
            throw new InvalidOperationException("AI service not initialized. Call InitAsync() first.");
        }

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            Messages.Add(new(ChatRole.System, systemPrompt));
        }

        return chatClient
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .UseOpenTelemetry(
                loggerFactory: loggerFactory,
                configure: o => o.EnableSensitiveData = true)
            .Build();
    }

    public async Task<IList<McpClientTool>> GetMcpToolsAsync(string? name = "MyMcpServer")
    {
        logger.LogInformation("Getting MCP tools with name: {Name}", name ?? "MyMcpServer");

        if (client is null)
        {
            logger.LogError("AI service not initialized. Call InitAsync() first.");
            throw new InvalidOperationException("AI service not initialized. Call InitAsync() first.");
        }

        using IChatClient samplingClient = chatClient
            .AsIChatClient()
            .AsBuilder()
            .UseOpenTelemetry(
                loggerFactory: loggerFactory,
                configure: o => o.EnableSensitiveData = true)
            .Build();

        var mcpClient = await McpClientFactory.CreateAsync(
            new SseClientTransport(new()
            {
                Endpoint = new Uri(toolsEndpoint),
                Name = name
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

        return await mcpClient.ListToolsAsync();
    }
}
