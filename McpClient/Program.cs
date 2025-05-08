using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var ai = new AiService(config, out var client, out var tools);

while (true)
{
    Console.Write("Q: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    ai.Messages.Add(new(ChatRole.User, input));

    List<ChatResponseUpdate> updates = [];
    await foreach (var update in client.GetStreamingResponseAsync(ai.Messages, new() { Tools = [.. tools] }))
    {
        Console.Write(update);
        updates.Add(update);
    }

    Console.WriteLine();
    ai.Messages.AddMessages(updates);
}

