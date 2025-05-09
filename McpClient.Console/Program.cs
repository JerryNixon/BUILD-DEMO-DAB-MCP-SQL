using Microsoft.Extensions.Configuration;

using Spectre.Console;

var setup = $"""
You're an AI who helps insurance reps interact with their data to help customers and find savings. 
Limit your responses to the answer and only show data when requested. 
For new records, increment the largest existing Id. Ask for missing fields before inserting.
When asked to list things, list only the name or title to limit each to one line unless asked otherwise. 
If I tell you I just communicated with a customer, add a communication record. 
Your output is plain text so do not _underline_ or *bold* or respond in tables or emojis. 
Number format: 1,234 or $1,234. Date format: DDD, MMM dd, YYYY. Today's date is {DateTime.Now}.
""";

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var name = AnsiConsole.Ask<string>("[blue]What's your name:[/] ", "Jerry");

var ai = await new AiService(config)
    .InitAsync($"{setup} My name is {name}, refer to me by name.");

var chatHistory = new List<string>();

AnsiConsole.Clear();
AnsiConsole.Write(new Rule("[deepskyblue1]Agentic Chat[/]").Centered());

while (true)
{
    var input = AnsiConsole.Prompt(
        new TextPrompt<string>("[bold green]Q:[/] ")
            .PromptStyle("green")
            .AllowEmpty());

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    chatHistory.Add($"[green]{input}[/]");

    AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("grey"))
        .Start("Thinking...", ctx =>
        {
            var response = ai.ChatAsync(input).GetAwaiter().GetResult();
            chatHistory.Add($"[yellow]{response}[/]");
        });

    var historyPanel = new Panel(string.Join("\n\n", chatHistory))
        .Border(BoxBorder.Double)
        .Header("[bold blue]Conversation[/]")
        .Padding(1, 1)
        .Expand();

    AnsiConsole.Clear();
    AnsiConsole.Write(historyPanel);
}
