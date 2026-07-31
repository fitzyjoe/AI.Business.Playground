using System.Diagnostics;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Text;
using Lesson04.StructuredOutputs.Features.Conversations;
using Microsoft.Extensions.Options;

namespace Lesson04.StructuredOutputs.Infrastructure.Ai.Providers;

public sealed class OllamaProvider : IAiProvider
{
	private readonly OllamaApiClient _ollama;
	private readonly OllamaOptions _options;

	public OllamaProvider(
		HttpClient httpClient,
		IOptions<OllamaOptions> options)
	{
		_options = options.Value;
		_ollama = new OllamaApiClient(httpClient);
	}
	
	public async Task<AiChatResponse> SendAsync(
		AiChatRequest aiRequest,
		CancellationToken cancellationToken = default)
	{
		var model = aiRequest.Model ?? _options.Model;

		var messages = aiRequest.Messages.Select(ToOllamaMessage).ToList();

		var options = new RequestOptions();
		if (aiRequest.Temperature.HasValue)
		{
			options.Temperature = aiRequest.Temperature.Value;
		}
		
		if (aiRequest.MaxTokens.HasValue)
		{
			options.NumPredict = aiRequest.MaxTokens.Value;
		}
		
		var chatRequest = new ChatRequest
		{
			Model = model,
			Messages = messages,
			Options = options
		};
		
		var stopwatch = Stopwatch.StartNew();

		var sb = new StringBuilder();
		await foreach (var response in _ollama.ChatAsync(chatRequest, cancellationToken))
		{
			if (response?.Message?.Content != null)
			{
				sb.Append(response.Message.Content);
			}
		}
		
		stopwatch.Stop();

		return new AiChatResponse(sb.ToString(), model, stopwatch.Elapsed);
	}
	
	private static Message ToOllamaMessage(
		ConversationMessage message)
	{
		return new Message
		{
			Role = message.Role switch
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
			},

			Content = message.Content
		};
	}
}