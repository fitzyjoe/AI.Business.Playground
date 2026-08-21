using System.Text.Json;
using System.Text.Json.Serialization;
using Lesson05.McpFundamentals.Features.Properties;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;

var builder = Host.CreateApplicationBuilder(args);

var mcpJsonOptions = new JsonSerializerOptions(McpJsonUtilities.DefaultOptions)
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.Never
	};

builder.Services
	.AddMcpServer()
	.WithStdioServerTransport()
	.WithTools<PropertyTools>(mcpJsonOptions);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
	// Standard output belongs exclusively to MCP messages.
	options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Services.AddSingleton<IPropertyRepository, InMemoryPropertyRepository>();
await builder.Build().RunAsync();

