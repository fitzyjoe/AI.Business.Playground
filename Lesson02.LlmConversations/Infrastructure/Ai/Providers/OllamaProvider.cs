using System.Diagnostics;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Text;
using Lesson02.LlmConversations.Features.Models.Execute;
using Microsoft.Extensions.Options;

namespace Lesson02.LlmConversations.Infrastructure.Ai.Providers;

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
	
	public async Task<AiResponse> SendAsync(
		AiRequest aiRequest,
		CancellationToken cancellationToken = default)
	{
		var model = aiRequest.Model ?? _options.Model;
		var messages = CreateMessages(aiRequest);
		
		var options = new RequestOptions
		{
			Temperature = aiRequest.Temperature
		};
		
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

		return new AiResponse(sb.ToString(), _ollama.SelectedModel, stopwatch.Elapsed);
	}
	
	private static List<Message> CreateMessages(AiRequest request)
	{
		var messages = new List<Message>();

		if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
		{
			messages.Add(new Message
			{
				Role = ChatRole.System,
				Content = request.SystemPrompt
			});
		}

		messages.Add(new Message
		{
			Role = ChatRole.User,
			Content = request.Prompt
		});

		return messages;
	}
}