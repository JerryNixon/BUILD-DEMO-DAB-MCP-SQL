using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication
    .CreateBuilder(args);

builder
    .ConfigureFunctionsWebApplication()
    .EnableMcpToolMetadata();

/*

builder.ConfigureMcpTool(GetStarDateTool.ToolName)
    .WithProperty(GetStarDateTool.DateName, GetStarDateTool.DateType, GetStarDateTool.DateDescription);

builder.ConfigureMcpTool(GetCustomersTool.ToolName)
    .WithProperty(GetCustomersTool.FilterName, GetCustomersTool.FilterType, GetCustomersTool.FilterDescription);

builder.ConfigureMcpTool(GetClaimsTool.ToolName)
    .WithProperty(GetClaimsTool.FilterName, GetClaimsTool.FilterType, GetClaimsTool.FilterDescription);

builder.ConfigureMcpTool(GetPoliciesTool.ToolName)
    .WithProperty(GetPoliciesTool.FilterName, GetPoliciesTool.FilterType, GetPoliciesTool.FilterDescription);

builder.ConfigureMcpTool(GetCommunicationHistoryTool.ToolName)
    .WithProperty(GetCommunicationHistoryTool.FilterName, GetCommunicationHistoryTool.FilterType, GetCommunicationHistoryTool.FilterDescription);

*/

builder.Build().Run();
