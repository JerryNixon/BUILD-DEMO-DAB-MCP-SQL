using Azure;
using Azure.AI.OpenAI;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

using System.Diagnostics;
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

        loggerFactory = LoggerFactory.Create(builder => { /* no logging */ });
        logger = loggerFactory.CreateLogger<AiService>();

        logger.LogInformation("AI service constructed with endpoint: {Endpoint} and tools: {Tools}", azureOpenAiEndpoint, toolsEndpoint);
    }

    public static async Task<bool> PingAsync(string url)
    {
        try
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };

            using var http = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(2) // hard timeout
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1)); // early cancel
            using var req = new HttpRequestMessage(HttpMethod.Get, url);

            using var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            Console.WriteLine($"Ping to {url} returned: {res.StatusCode}");
            return res.IsSuccessStatusCode;
        }
        catch (TaskCanceledException)
        {
            Debugger.Break();
            Console.WriteLine($"Ping to {url} timed out.");
            return false;
        }
        catch (Exception ex)
        {
            Debugger.Break();
            Console.WriteLine($"Ping to {url} failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> PingOpenAiAsync()
    {
        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{azureOpenAiEndpoint}openai/deployments/{deploymentName}/chat/completions?api-version=2024-02-15-preview");
        request.Headers.Add("api-key", openAiKey);
        request.Content = new StringContent("""
        {
            "messages": [{"role":"user", "content":"ping"}],
            "max_tokens": 1
        }
        """, Encoding.UTF8, "application/json");

        try
        {
            using var response = await http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debugger.Break();
            return false;
        }
    }


    public async Task<AiService> InitAsync(string? systemPrompt = null)
    {
        logger.LogInformation("Initializing AI service with system prompt: {SystemPrompt}", systemPrompt ?? "null");

        if (!await PingAsync(toolsEndpoint))
        {
            throw new Exception($"Ping to toolsEndpoint failed: {toolsEndpoint}");
        }

        if (!await PingOpenAiAsync())
        {
            throw new Exception($"Ping to azureOpenAiEndpoint failed: {azureOpenAiEndpoint}");
        }

        openAiClient = new AzureOpenAIClient(
            endpoint: new Uri(azureOpenAiEndpoint),
            credential: new AzureKeyCredential(openAiKey));

        chatClient = openAiClient.GetChatClient(deploymentName);

        client = GetChatClient(systemPrompt);

        var t = await GetMcpToolsAsync();
        tools.AddRange(t);

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
            loggerFactory: loggerFactory);

        return await mcpClient.ListToolsAsync();
    }
}
