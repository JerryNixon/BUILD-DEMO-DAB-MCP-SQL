using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.Text.Json;
using System.Text;
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class ChatProxy
{
    private readonly HttpClient _httpClient = new();

    public ChatProxy(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new();
    }

    [Function("ChatProxy")]
    public async Task<HttpResponseData> RunChatProxy(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chatproxy")] HttpRequestData req,
        FunctionContext context)
    {
        ILogger logger = context.GetLogger("ChatProxy");
        logger.LogInformation("Entered ChatProxy function.");

        string endpoint = GetEnvironmentVariableOrThrow("AZURE_OPENAI_ENDPOINT", logger);
        string key = GetEnvironmentVariableOrThrow("AZURE_OPENAI_KEY", logger);
        string deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "gpt-4o";

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);

        try
        {
            ChatRequest input = await DeserializeChatRequestAsync(req, logger);
            if (input.Messages is null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Invalid input: messages missing.");
                return response;
            }

            List<ChatMessage> sdkMessages = ConvertToChatMessages(input.Messages, logger);
            ChatCompletionOptions options = CreateChatCompletionOptions(input, logger);

            AzureOpenAIClient client = new(new Uri(endpoint), new AzureKeyCredential(key));
            ChatClient chatClient = client.GetChatClient(deployment);
            logger.LogInformation("Calling OpenAI chat completion.");
            ChatCompletion completion = await chatClient.CompleteChatAsync(sdkMessages, options);

            logger.LogInformation("Received OpenAI response.");
            await response.WriteStringAsync(JsonSerializer.Serialize(completion, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in ChatProxy.");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync($"OpenAI proxy error: {ex.Message}");
        }

        return response;

        static string GetEnvironmentVariableOrThrow(string name, ILogger logger)
        {
            logger.LogInformation("Retrieving environment variable: {Name}", name);
            return Environment.GetEnvironmentVariable(name)
                ?? throw new InvalidOperationException($"Missing env var: {name}");
        }

        static async Task<ChatRequest> DeserializeChatRequestAsync(HttpRequestData req, ILogger logger)
        {
            logger.LogInformation("Deserializing ChatRequest from request body.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            ChatRequest? input = JsonSerializer.Deserialize<ChatRequest>(requestBody);
            return input ?? throw new InvalidOperationException("Unable to deserialize ChatRequest.");
        }

        static List<ChatMessage> ConvertToChatMessages(List<SerializableChatMessage> messages, ILogger logger)
        {
            logger.LogInformation("Converting SerializableChatMessages to ChatMessages.");
            return messages.Select<SerializableChatMessage, ChatMessage>(m =>
                m.Role?.ToLowerInvariant() switch
                {
                    "system" => new SystemChatMessage(m.Content ?? ""),
                    "user" => new UserChatMessage(m.Content ?? ""),
                    "assistant" => new AssistantChatMessage(m.Content ?? ""),
                    _ => throw new InvalidOperationException($"Unsupported role: {m.Role}")
                }).ToList();
        }

        static ChatCompletionOptions CreateChatCompletionOptions(ChatRequest input, ILogger logger)
        {
            logger.LogInformation("Creating ChatCompletionOptions.");
            return new ChatCompletionOptions
            {
                Temperature = input.Temperature ?? 0.7f,
                MaxOutputTokenCount = input.MaxTokens ?? 800
            };
        }
    }

    [Function("TestProxy")]
    public async Task<HttpResponseData> RunTestProxy(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "testproxy")] HttpRequestData req,
        FunctionContext context)
    {
        ILogger logger = context.GetLogger("TestProxy");
        logger.LogInformation("Entered TestProxy function.");

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);

        try
        {
            string baseUri = Environment.GetEnvironmentVariable("SWABASEURL") ?? "http://localhost:7071";
            string requestJson = CreateTestPayload(logger);

            HttpResponseMessage result = await CallChatProxyAsync($"{baseUri}/api/chatproxy", requestJson, logger);
            string resultContent = await result.Content.ReadAsStringAsync();

            if (!result.IsSuccessStatusCode)
            {
                logger.LogWarning("ChatProxy returned failure: {Status}", result.StatusCode);
                response.StatusCode = result.StatusCode;
                await response.WriteStringAsync($"ChatProxy failed: {resultContent}");
                return response;
            }

            logger.LogInformation("ChatProxy call succeeded.");
            await response.WriteStringAsync($"ChatProxy success:\n{resultContent}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in TestProxy.");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync($"Exception calling ChatProxy: {ex.Message}");
        }

        return response;

        static string CreateTestPayload(ILogger logger)
        {
            logger.LogInformation("Creating test payload.");
            object payload = new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are an AI assistant that helps people find information." },
                    new { role = "user", content = "Say hello in one word" }
                },
                temperature = 0.7,
                max_tokens = 800
            };
            return JsonSerializer.Serialize(payload);
        }

        async Task<HttpResponseMessage> CallChatProxyAsync(string url, string json, ILogger logger)
        {
            logger.LogInformation("Sending request to ChatProxy: {Url}", url);
            HttpRequestMessage httpRequest = new(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return await _httpClient.SendAsync(httpRequest);
        }
    }

    public class ChatRequest
    {
        [JsonPropertyName("messages")]
        public List<SerializableChatMessage>? Messages { get; set; }

        [JsonPropertyName("temperature")]
        public float? Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }
    }

    public class SerializableChatMessage
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
