using Lesson01.Chat.Features.Models.Execute;
using OllamaSharp;
using OllamaSharp.Models.Chat;
using System.Text;
using Microsoft.Extensions.Options;
using OllamaSharp.Models;

namespace Lesson01.Chat.Infrastructure.Ai.Providers;

public sealed class OllamaProvider : IAiProvider
{
	private readonly OllamaApiClient _ollama;

	public OllamaProvider(HttpClient httpClient, IOptions<OllamaOptions> options)
	{
		httpClient.BaseAddress = new Uri(options.Value.Endpoint);
		_ollama = new OllamaApiClient(httpClient)
		{
			SelectedModel = options.Value.Model
		};
	}
	
	public async Task<AiResponse> SendAsync(
		AiRequest aiRequest,
		CancellationToken cancellationToken = default)
	{
		var sb = new StringBuilder();
		var chatRequest = new ChatRequest
		{
			Messages = [new Message { Role = ChatRole.User, Content = aiRequest.Prompt },
				new Message { Role = ChatRole.System, Content = aiRequest.SystemPrompt }],
			Options = new RequestOptions
			{
				Temperature = aiRequest.Temperature,
				NumPredict = aiRequest.MaxTokens ?? -1
			}
		};
		
		var startTime = DateTime.Now;

		await foreach (var response in _ollama.ChatAsync(chatRequest, cancellationToken))
		{
			if (response?.Message?.Content != null)
			{
				sb.Append(response.Message.Content);
			}
		}
		
		var endTime = DateTime.Now;

		return new AiResponse(sb.ToString(), _ollama.SelectedModel, endTime.Subtract(startTime));
	}
}