using System.Diagnostics;
using Lesson06.ConsumingMcpServers.Features.Conversations;
using Lesson06.ConsumingMcpServers.Infrastructure.Mcp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAiChatClient = OpenAI.Chat.ChatClient;

namespace Lesson06.ConsumingMcpServers.Infrastructure.Ai.Providers;

public sealed class OpenAiProvider(
	IOptions<OpenAiOptions> options,
	PropertyMcpClient propertyMcpClient) : IAiProvider
{
	private readonly OpenAiOptions _options = options.Value;
	private readonly PropertyMcpClient _propertyMcpClient = propertyMcpClient;

	public async Task<AiChatResponse> SendAsync(
		AiChatRequest aiRequest,
		CancellationToken cancellationToken = default)
	{
		var model = aiRequest.Model ?? _options.Model;
		var apiKey = Environment.GetEnvironmentVariable("OPENAI_AI_BUSINESS_PLAYGROUND")
		             ?? throw new InvalidOperationException(
			             "OPENAI_AI_BUSINESS_PLAYGROUND environment variable is required.");

		using IChatClient chatClient = new OpenAiChatClient(model, apiKey)
			.AsIChatClient()
			.AsBuilder()
			.UseFunctionInvocation(configure: options =>
			{
				options.MaximumIterationsPerRequest = 6;
			})
			.Build();

		var messages = aiRequest.Messages.Select(ToChatMessage).ToList();
		var chatOptions = new ChatOptions
		{
			Temperature = aiRequest.Temperature,
			MaxOutputTokens = aiRequest.MaxTokens,
			Tools = [.. _propertyMcpClient.Tools]
		};

		var stopwatch = Stopwatch.StartNew();
		var response = await chatClient.GetResponseAsync(
			messages,
			chatOptions,
			cancellationToken);
		stopwatch.Stop();

		return new AiChatResponse(response.Text, model, stopwatch.Elapsed);
	}

	private static ChatMessage ToChatMessage(ConversationMessage message)
	{
		var role = message.Role switch
		{
			ConversationRole.System => ChatRole.System,
			ConversationRole.User => ChatRole.User,
			ConversationRole.Assistant => ChatRole.Assistant,
			_ => throw new ArgumentOutOfRangeException(
				nameof(message.Role),
				message.Role,
				"Unsupported conversation role.")
		};

		return new ChatMessage(role, message.Content);
	}
}
