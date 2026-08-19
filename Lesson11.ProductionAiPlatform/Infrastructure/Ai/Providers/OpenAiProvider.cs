using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Lesson11.ProductionAiPlatform.Infrastructure.Ai.Providers;

public sealed class OpenAiProvider : IAiProvider, IDisposable
{
	private readonly OpenAiOptions _options;

	public OpenAiProvider(
		IOptions<OpenAiOptions> options,
		IOptions<AiOptions> aiOptions,
		ILoggerFactory loggerFactory)
	{
		_options = options.Value;

		var apiKey = Environment.GetEnvironmentVariable("OPENAI_AI_BUSINESS_PLAYGROUND")
		             ?? throw new InvalidOperationException(
			             "OPENAI_AI_BUSINESS_PLAYGROUND environment variable is required.");

		IChatClient client = new ChatClient(model: _options.Model, apiKey: apiKey).AsIChatClient();
		client = new BoundedChatClient(client, aiOptions);

		ChatClient = client
			.AsBuilder()
			.UseOpenTelemetry(
				loggerFactory,
				AiTelemetry.SourceName,
				telemetry =>
				{
					telemetry.EnableSensitiveData = false;
				})
			.Build();
	}

	public string Name => "openai";
	public string DefaultModel => _options.Model;
	public IChatClient ChatClient { get; }

	public void Dispose()
	{
		ChatClient.Dispose();
	}
}
