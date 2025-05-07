using Client.Components;

var builder = WebApplication.CreateBuilder(args);

// read from environment or configuration
var endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"];
var key = builder.Configuration["AZURE_OPENAI_KEY"];

builder.Services.AddHttpClient("AzureOpenAI", client =>
{
    client.BaseAddress = new Uri(endpoint!);
    client.DefaultRequestHeaders.Add("api-key", key!); // Azure OpenAI expects this header
});

// optional: inject typed HttpClient if desired
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("AzureOpenAI"));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
