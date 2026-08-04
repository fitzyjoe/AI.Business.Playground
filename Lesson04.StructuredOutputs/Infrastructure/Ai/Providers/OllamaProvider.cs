using System.Diagnostics;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Text;
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

		// var messages = aiRequest.Messages.Select(ToOllamaMessage).ToList();

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
			Messages = aiRequest.Messages.Select(ToOllamaMessage).ToList(),
			Options = options,
			Format = aiRequest.ResponseFormat,
			Stream = aiRequest.Stream
		};
		
		var stopwatch = Stopwatch.StartNew();

		var responseText = new StringBuilder();
		await foreach (var response in _ollama.ChatAsync(chatRequest, cancellationToken))
		{
			if (response?.Message?.Content != null)
			{
				responseText.Append(response.Message.Content);
			}
		}
		
		stopwatch.Stop();

		return new AiChatResponse(responseText.ToString(), model, stopwatch.Elapsed);
	}
	
	private static Message ToOllamaMessage(
		AiChatMessage message)
	{
		return new Message
		{
			Role = message.Role switch
			{
				AiMessageRole.System => ChatRole.System,
				AiMessageRole.User => ChatRole.User,
				AiMessageRole.Assistant => ChatRole.Assistant,

				_ => throw new ArgumentOutOfRangeException(
					nameof(message.Role),
					message.Role,
					"Unsupported AI message role.")
			},

			Content = message.Content
		};
	}
}