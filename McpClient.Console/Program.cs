using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var setup = "You help insurance reps interact with their data to help customers and find savings. " +
        "Limit your responses to the answer and only show data when requested. " +
        "When asked to list things, list only the name or title to limit each to one line unless asked otherwise. " +
        "Your output is plain text so do not _underline_ or *bold* or respond in tables. " +
        "Should an error occur, try one more time, then report the error as terse as possible. ";
var ai = new AiService(config, out var client, out var tools, setup);

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

