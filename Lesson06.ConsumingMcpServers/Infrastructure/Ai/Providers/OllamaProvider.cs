using System.Diagnostics;
using OllamaSharp;
using Lesson06.ConsumingMcpServers.Features.Conversations;
using Lesson06.ConsumingMcpServers.Infrastructure.Mcp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using AiChatMessage = Microsoft.Extensions.AI.ChatMessage;
using AiChatRole = Microsoft.Extensions.AI.ChatRole;

namespace Lesson06.ConsumingMcpServers.Infrastructure.Ai.Providers;

public sealed class OllamaProvider : IAiProvider
{
	private readonly OllamaApiClient _ollama;
	private readonly OllamaOptions _options;
	private readonly PropertyMcpClient _propertyMcpClient;
	private readonly IChatClient _chatClient;

	public OllamaProvider(
		HttpClient httpClient,
		IOptions<OllamaOptions> options,
		PropertyMcpClient propertyMcpClient)
	{
		_options = options.Value;
		_ollama = new OllamaApiClient(httpClient);
		_propertyMcpClient = propertyMcpClient;
		
		IChatClient chatClient = _ollama;
		_chatClient = chatClient
			.AsBuilder()
			.UseFunctionInvocation(configure: options =>
			{
				options.MaximumIterationsPerRequest = 6;
			})
			.Build();
	}
	
	public async Task<AiChatResponse> SendAsync(
		AiChatRequest aiRequest,
		CancellationToken cancellationToken = default)
	{
		var model = aiRequest.Model ?? _options.Model;

		var messages = aiRequest.Messages.Select(ToChatMessage).ToList();
		
		var chatOptions = new ChatOptions
		{
			ModelId = model,
			Temperature = aiRequest.Temperature,
			MaxOutputTokens = aiRequest.MaxTokens,
			Tools = [.. _propertyMcpClient.Tools]
		};
		
		var stopwatch = Stopwatch.StartNew();

		var response = await _chatClient.GetResponseAsync(
			messages,
			chatOptions,
			cancellationToken);
		
		stopwatch.Stop();

		return new AiChatResponse(response.Text, model, stopwatch.Elapsed);
	}
	
	private static ChatMessage ToChatMessage(
		ConversationMessage message)
	{
		ChatRole role = message.Role switch
		{
			ConversationRole.System =>
				ChatRole.System,

			ConversationRole.User =>
				ChatRole.User,

			ConversationRole.Assistant =>
				ChatRole.Assistant,

			_ => throw new ArgumentOutOfRangeException(
				nameof(message.Role),
				message.Role,
				"Unsupported conversation role.")
		};

		return new ChatMessage(role, message.Content);
	}
}