using ModelContextProtocol.Client;

namespace Lesson09.Agents.Infrastructure.Mcp;

public sealed class PropertyMcpClient : IAsyncDisposable
{
	private McpClient? _client;

	public IReadOnlyList<McpClientTool> Tools { get; private set; } = [];

	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		if (_client is not null)
		{
			return;
		}

		var lesson05Directory = Path.GetFullPath("../../../../Lesson05.McpFundamentals", AppContext.BaseDirectory);
		var transport = new StdioClientTransport(
			new StdioClientTransportOptions
			{
				Name = "Property Records",
				Command = "dotnet",
				Arguments = ["./bin/Debug/net10.0/Lesson05.McpFundamentals.dll"],
				WorkingDirectory = lesson05Directory,
				StandardErrorLines = line => Console.Error.WriteLine($"[Lesson05] {line}")
			});

		_client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);

		Tools =
		[
			.. await _client.ListToolsAsync(cancellationToken: cancellationToken)
		];
	}

	public async ValueTask DisposeAsync()
	{
		if (_client is not null)
		{
			await _client.DisposeAsync();
		}
	}
}