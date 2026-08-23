using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAiChatClient = OpenAI.Chat.ChatClient;

namespace Lesson09.Agents.Infrastructure.Ai.Providers;

public sealed class OpenAiProvider : IAiProvider, IDisposable
{
	private readonly OpenAiOptions _options;
	private readonly Lazy<IChatClient> _chatClient;

	public OpenAiProvider(IOptions<OpenAiOptions> options)
	{
		_options = options.Value;
		_chatClient = new Lazy<IChatClient>(CreateChatClient);
	}

	public string Name => "openai";
	public string DefaultModel => _options.Model;
	public IChatClient ChatClient => _chatClient.Value;

	private IChatClient CreateChatClient()
	{
		var apiKey = Environment.GetEnvironmentVariable("OPENAI_AI_BUSINESS_PLAYGROUND")
		             ?? throw new InvalidOperationException(
			             "OPENAI_AI_BUSINESS_PLAYGROUND environment variable is required.");

		return new OpenAiChatClient(_options.Model, apiKey).AsIChatClient();
	}

	public void Dispose()
	{
		if (_chatClient.IsValueCreated)
		{
			_chatClient.Value.Dispose();
		}
	}
}
