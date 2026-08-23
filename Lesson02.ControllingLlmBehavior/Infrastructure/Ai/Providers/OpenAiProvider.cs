using System.Diagnostics;
using Lesson02.ControllingLlmBehavior.Features.Models.Execute;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAiChatClient = OpenAI.Chat.ChatClient;

namespace Lesson02.ControllingLlmBehavior.Infrastructure.Ai.Providers;

public sealed class OpenAiProvider(IOptions<OpenAiOptions> options) : IAiProvider
{
	private readonly OpenAiOptions _options = options.Value;

	public async Task<AiResponse> SendAsync(
		AiRequest aiRequest,
		CancellationToken cancellationToken = default)
	{
		var model = aiRequest.Model ?? _options.Model;
		var apiKey = Environment.GetEnvironmentVariable("OPENAI_AI_BUSINESS_PLAYGROUND")
		             ?? throw new InvalidOperationException(
			             "OPENAI_AI_BUSINESS_PLAYGROUND environment variable is required.");

		using IChatClient chatClient = new OpenAiChatClient(model, apiKey).AsIChatClient();

		var messages = new List<ChatMessage>();
		if (!string.IsNullOrWhiteSpace(aiRequest.SystemPrompt))
		{
			messages.Add(new ChatMessage(ChatRole.System, aiRequest.SystemPrompt));
		}

		messages.Add(new ChatMessage(ChatRole.User, aiRequest.Prompt));

		var chatOptions = new ChatOptions
		{
			Temperature = aiRequest.Temperature,
			MaxOutputTokens = aiRequest.MaxTokens
		};

		var stopwatch = Stopwatch.StartNew();
		var response = await chatClient.GetResponseAsync(
			messages,
			chatOptions,
			cancellationToken);
		stopwatch.Stop();

		return new AiResponse(response.Text, model, stopwatch.Elapsed);
	}
}
