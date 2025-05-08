using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using Azure.AI.OpenAI;
using Azure;

// Load configuration
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var azureOpenAiEndpoint = config["AZURE_OPENAI_ENDPOINT"] ?? throw new InvalidOperationException("Missing AZURE_OPENAI_ENDPOINT");
var openAiKey = config["OPENAI_API_KEY"] ?? throw new InvalidOperationException("Missing OPENAI_API_KEY");
var deploymentName = config["AZURE_OPENAI_DEPLOYMENT"] ?? "gpt-4o";

// OpenTelemetry setup
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddHttpClientInstrumentation()
    .AddSource("*")
    .AddOtlpExporter()
    .Build();

using var metricsProvider = Sdk.CreateMeterProviderBuilder()
    .AddHttpClientInstrumentation()
    .AddMeter("*")
    .AddOtlpExporter()
    .Build();

using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddOpenTelemetry(opt => opt.AddOtlpExporter()));

// Create AzureOpenAI client
var openAiClient = new AzureOpenAIClient(new Uri(azureOpenAiEndpoint), new AzureKeyCredential(openAiKey));
var chatClient = openAiClient.GetChatClient(deploymentName);

// Create sampling client
using IChatClient samplingClient = chatClient.AsIChatClient()
    .AsBuilder()
    .UseOpenTelemetry(loggerFactory: loggerFactory, configure: o => o.EnableSensitiveData = true)
    .Build();

// Create MCP client
var mcpClient = await McpClientFactory.CreateAsync(
    new StdioClientTransport(new()
    {
        Command = "npx",
        Arguments = ["-y", "--verbose", "@modelcontextprotocol/server-everything"],
        Name = "Everything"
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

// List tools
Console.WriteLine("Tools available:");
var tools = await mcpClient.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"  {tool}");
}

Console.WriteLine();

// Create tool-aware chat client
using IChatClient toolAwareChat = chatClient.AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .UseOpenTelemetry(loggerFactory: loggerFactory, configure: o => o.EnableSensitiveData = true)
    .Build();

// Chat loop
List<ChatMessage> messages = [];
while (true)
{
    Console.Write("Q: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) break;

    messages.Add(new(ChatRole.User, input));

    List<ChatResponseUpdate> updates = [];
    await foreach (var update in toolAwareChat.GetStreamingResponseAsync(messages, new() { Tools = [.. tools] }))
    {
        Console.Write(update);
        updates.Add(update);
    }

    Console.WriteLine();
    messages.AddMessages(updates);
}
